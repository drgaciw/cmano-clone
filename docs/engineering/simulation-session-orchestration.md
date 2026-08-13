# SimulationSession orchestration — developer guide

`SimulationSession` is the **headless per-tick conductor** that sits between the engine-agnostic
delegation core and the sim engagement pipeline. It is what the replay harness, the QA Gauntlet, the
console demo, and the Unity `DelegationBridge` all drive: one call to `Tick(state)` /
`TickHeadless(state)` runs the delegation decision tick, extracts the engage orders it produced, gates
and deconflicts them, resolves the engagement pipeline for the tick, folds every outcome into the
order log, then applies the post-engage hot-tick systems (catalog damage, BDA lifecycle, logistics
FSMs, ordnance/fuel bands). It owns the sim clock, the pause/watch state, and the per-tick composition
of otherwise-independent subsystems.

This page documents what the session actually **does** each tick, its public seams, and the ordering
guarantees that keep replay deterministic. It is verified against source and pinned by the tests
listed at the end.

- **Source:** [`src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs).
- **Sim runner it drives:** [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs)
  (ADR-004 tick runner + engagement step) and [`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs).
- **Decision core it wraps:** `DelegationOrchestrator.Tick` — see
  [agent-decision-pipeline.md](agent-decision-pipeline.md).
- **Related subsystem pages this page ties together:** the kill chain
  ([engagement-pipeline.md](engagement-pipeline.md)), catalog damage
  ([catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)), fuel/logistics
  ([logistics-fuel-runtime.md](logistics-fuel-runtime.md)), comms
  ([comms-degradation-runtime.md](comms-degradation-runtime.md)), swarm salvo deconfliction
  ([swarm-runtime.md](swarm-runtime.md)), balance drift
  ([balance-drift-telemetry.md](balance-drift-telemetry.md)), and the headless runner that hosts the
  session ([baltic-replay-harness.md](baltic-replay-harness.md)). Determinism rules live in
  [determinism-and-replay.md](determinism-and-replay.md).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when extending the session.

| Invariant | Rule |
|-----------|------|
| **Decision, then execution** | Every tick calls `Orchestrator.Tick(state)` **first**; only its `ExecutedOrders` are eligible for the engagement/logistics phase. The session never fires an order the delegation gate did not execute. |
| **Deterministic composition** | The session adds *no* randomness of its own — all draws live in `DelegationOrchestrator` and the `IEngagementResolver`. Iteration is order-stable (executed-order order; sorted deconfliction/BDA), so the folded order log is a pure function of `(seed, scenario, inputs)`. |
| **Pause ≠ stall for headless** | Interactive `Tick` respects `SimClock.IsPaused` (decision runs, engagement phase early-returns after surfacing ROE denials). `TickHeadless` sets `HeadlessBatch` and advances the engagement pipeline regardless, preserving the pause flag/reason so CI/replay never stall and interactive resume still works. |
| **Order log is the source of truth** | Engagements, magazine changes, outcomes, ordnance bands, platform-damage changes, and policy denials are appended to `Orchestrator.OrderLog` / `DecisionLog` with `SequenceId: 0` (the log assigns the real sequence). Read models project from there; nothing bypasses it. |
| **Bridge stays zero-touch** | The session is the headless composition point precisely so `DelegationBridge.cs` stays a thin facade. New per-tick behaviour goes here (or in a sub-applier the session calls), not in the hotpath Bridge. |
| **Optional subsystems fail closed** | Catalog damage, BDA lifecycle, balance drift, fuel, and air/boat ops are nullable and only run when their dependency is bound (`CatalogReader`, `CatalogDamageHotTickTracker`, etc.). A missing dependency is a no-op, never an exception. |

---

## Construction: binding a session

| Factory | Use |
|---------|-----|
| `new SimulationSession(globalSeed, engagement?, policyEvaluator?)` | Bare session over a fresh `DelegationOrchestrator` + `SimTickPipeline` (defaults to `StubEngagementResolver`). |
| `BindMvpEngagement(orchestrator, defaultEngageContext, defaultMagazineRounds, catalogReader?)` | Wires the full **MVP engagement stack** onto an existing orchestrator: `DictionaryEngageWorldQuery`, `MagazineLedger`, `KilledTargetRegistry`, an `MvpEngagementResolver` (seeded from the scenario, using the orchestrator's `PolicyEvaluator` + `ResolveEffectivePolicyForUnit` + speculative/engage defaults), plus the balance-drift consumer, catalog-damage tracker, and BDA lifecycle registry (the last two only when combat domains are enabled). |
| `BindMvpEngagementForScenario(orchestrator, scenarioPolicyId, catalog?, weaponId?)` | Resolves the `EngageContext` from the scenario policy (`ScenarioPolicyRepository`), applies the catalog weapon envelope (`CatalogEngageEnvelope.Apply`), reads the magazine rounds from `EngageDefaults`, then delegates to `BindMvpEngagement`. This is the path the replay harness / Bridge use. |
| `CreateWithMvpEngagement(globalSeed, …)` | Convenience for tests/demo: builds the orchestrator + MVP stack over the Baltic patrol fixture. |

The mutable/`init` seams that scenarios and the harness set after binding include `FuelTimeline`,
`UnitReadiness`, `AirOps` / `BoatOps`, `NextEngageSalvoOverride`, and `IsContactSpoofed`.

---

## The executing tick — `RunExecutingTick`

Both `Tick` (interactive) and `TickHeadless` route to `RunExecutingTick(state, headlessOverride)`
after a `Planning`-phase guard (`Tick` returns `false` while planning). The ordered pipeline:

```
Orchestrator.Tick(state)                         ← delegation decision → ExecutedOrders + logs
   │
   ├─ if Clock.IsPaused && !headlessOverride:
   │     SurfaceRoePolicyDeniedEngagements(); return   ← interactive pause: no engagement phase
   │
   ├─ collect ExecutedOrders where Kind == Engage
   ├─ commsBlocksEngage = CommsStateProjection.BlocksNewEngagement(project(DecisionLog))
   ├─ SwarmSalvoDeconfliction.Allocate(slots)          ← one shooter per (shooter,target)
   │
   ├─ for each engage order:
   │     comms blocked?  → append PolicyDenial(CommsDenied); skip
   │     not an accepted deconfliction pair? → skip
   │     resolve victim → PrimeEngageWorld(request) → Sim.EnqueueEngagement(request); queue it
   │
   ├─ Sim.TickOnce(mode)                                ← mode = HeadlessBatch | RealTime; resolves engagements
   ├─ LogEngagementResults(queued)                      ← EngagementRecord + magazine + outcome + kill registry + balance drift
   ├─ SurfaceRoePolicyDeniedEngagements()               ← WeaponsTight denials → EngagementRecord(Launched:false)
   ├─ ApplyCatalogDamageHotTick(queued)                 ← HP ledger, withdraw trials, BDA lifecycle → own-side watch loss
   ├─ for i in [0 .. AccelerationFactor-1): Sim.TickOnce(mode)   ← extra compressed steps
   └─ AdvanceLogisticsFsms(state)                       ← launch/recover orders, AirOps/BoatOps TickAll(1)
```

Key points:

- **Comms gate.** When `CommsStateProjection` reports `Denied`, every new engage order is turned into
  a `PolicyDenialRecord(FireAbortReason.CommsDenied)` instead of being queued — see
  [comms-degradation-runtime.md](comms-degradation-runtime.md).
- **Swarm salvo deconfliction.** Orders are reduced to one shooter per `(shooter, target)` pair via
  `SwarmSalvoDeconfliction.Allocate` (req 14) before any engagement is enqueued — see
  [swarm-runtime.md](swarm-runtime.md).
- **Priming.** `PrimeEngageWorld` composes the per-shot `EngageContext` from live state: fire-control
  track, EMCON (`ScenarioEmconResolver`), mobility/air readiness, catalog-damage withdraw block,
  fuel-bingo block, spoof flag (`IsContactSpoofed`), salvo size (`NextEngageSalvoOverride` one-shot),
  and shotgun threshold; it also seeds the magazine ledger and clamps to policy rounds.
- **Victim resolution.** `ResolveEngageVictim` maps a shooter to its target: red shooters target the
  primary blue contact (or default blue / `u1`); blue shooters use `PreferredHostileByShooter` when
  present, else the primary hostile (or default red / `hostile-1`).
- **Acceleration.** Time compression is expanded at the session level: one `TickOnce` plus
  `AccelerationFactor - 1` extra steps (rather than relying on `TimeCompressionMode.Accelerated`), so
  each compressed step runs a full engagement pass.

---

## What lands in the order log

`LogEngagementResults` and its helpers append (all with `SequenceId: 0`, resolved by the log):

| Record | When |
|--------|------|
| `EngagementRecord` | Every queued engagement — `Launched` true, or an abort code from `EngagementAbortReasonCodes` (incl. `NoResult` when the resolver produced fewer results than queued). |
| `MagazineChangeRecord` (`Fire`, `Delta = -salvo`) | On launch; salvo size read back from the primed `EngageContext`. |
| `OrdnanceStateChangeRecord` | On a band change only (`OrdnanceStateBands` vs the per-unit `_lastOrdnanceBand` memo). |
| `EngagementOutcomeRecord` | On launch with an outcome code (Hit/Intercept/Kill + `PkDraw`). |
| `KilledTargetRegistry.MarkKilled` | On a `Kill` outcome (feeds the world-hash kill mix). |
| `BalanceDriftConsumer.RecordEngagementOutcome` | On launch (advisory telemetry — see [balance-drift-telemetry.md](balance-drift-telemetry.md)). |
| `EngagementRecord(Launched:false, WeaponsTight)` | Via `SurfaceRoePolicyDeniedEngagements` for ROE `WeaponsTight` denials at this tick. |
| `PlatformDamageChangeRecord` | Via `ApplyCatalogDamageHotTick` (see below). |

---

## Post-engage hot-tick systems

- **Catalog damage / readiness** (`ApplyCatalogDamageHotTick`): only when a `CatalogDamageHotTickTracker`
  and `CatalogReader` are bound. Maps launched outcomes to `CatalogDamageHotTickApplier.OutcomeApply`
  (Hit → `CombatDamageLevel.DefaultHitSeverity`), applies the tick, appends each
  `PlatformDamageChangeRecord`, rebinds withdraw trials (`BindCatalogWithdrawTrials`), and runs the
  BDA lifecycle step. Full model: [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md).
- **BDA contact lifecycle** (`ApplyBdaContactLifecycleHotTick`): when enabled, maps damage changes to
  sorted lost targets, marks them `Lost` in the `BdaContactLifecycleRegistry`, and reports **own-side**
  losses to the watch/auto-pause queue via `ReportOwnSideLoss(targetId, tick, "bda:lost")` (hostile
  losses stay silent here).
- **Logistics FSMs** (`AdvanceLogisticsFsms`): processes `LaunchAircraft` / `AbortLaunchAircraft` /
  `LaunchBoat` / `RecoverBoat` / `AbortBoatLaunch` orders (lazily creating `AirOps` / `BoatOps` maps and
  unit state, honoring `UnitReadiness`), then advances both ops maps one step.

---

## Clock, pause & watch

The session owns the clock and the watch/auto-pause state (the watch queue itself is documented with
its emit rules under `Delegation/Watch/`):

- `PauseSim` / `ResumeSim` are thin `SimClock` wrappers; `IsSimPaused` exposes `Clock.IsPaused`.
- `SetTimeAccelerationFactor` / `TimeAccelerationFactor` proxy `SimClock` (clamped `1..256`).
- `ReportWatchAttention` enqueues an event and auto-pauses via `WatchAutoPauseGate`; `TryResumeSim`
  is the gated resume (blocked while unresolved pause-class cards remain, unless force-overridden).
  `ReportContactTransitions` / `ReportOwnSideLoss` are the pure emit call-sites the harness feeds.

---

## Determinism & replay

- The session contributes **no** RNG; every stochastic draw is inside `DelegationOrchestrator`
  (decision softmax) or the `IEngagementResolver` (combat outcome). See
  [determinism-and-replay.md](determinism-and-replay.md).
- Order extraction, deconfliction, and BDA resolution iterate in stable order (executed-order order;
  `SwarmSalvoDeconfliction` sorts by `(shooter, target, weapon)`; BDA uses `ResolveSortedLostTargets`),
  so the appended order log is byte-identical for a given seed/scenario.
- `SimTickPipeline` folds the core world hash, detection sub-hash, engagement mix, and kill mix into
  `LastWorldHash`; the session never touches that fold directly. The Baltic v2 replay hash
  `17144800277401907079` and ReplayGolden 6/6 must be preserved when extending the session.

---

## How to extend without breaking replay

1. **Add work as a helper the session calls** after `Sim.TickOnce`, not as new Bridge hotpath code.
   Keep `DelegationBridge.cs` zero-touch.
2. **Read from `ExecutedOrders`, write to the order log.** New effects must be derived from gated
   executed orders and emitted as order-log records (`SequenceId: 0`); do not mutate read models.
3. **Iterate deterministically.** Preserve executed-order order or sort explicitly; introduce no
   wall-clock or `Random.Shared`. Any randomness must come from the seeded resolver/orchestrator.
4. **Make new subsystems optional & fail-closed.** Bind them via `init`/settable properties and no-op
   when the dependency is absent, mirroring the catalog-damage / BDA / fuel pattern.
5. **Respect the pause/headless contract.** Anything that must run for CI/replay belongs after the
   `headlessOverride` early-return; interactive-only surfacing (like ROE denial rows) belongs before it.
6. **Re-run the goldens.** `dotnet test ProjectAegis.sln`, the ReplayGolden suite, and the QA Gauntlet;
   a hash move must be intentional and the Baltic v2 hash must stay `17144800277401907079`.

---

## Consumers

| Consumer | Role |
|----------|------|
| [`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs) | Builds a session per run and drives `TickHeadless` for replay golden / Gauntlet / CLI — see [baltic-replay-harness.md](baltic-replay-harness.md). |
| [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | Unity-facing facade; binds a session via `BindMvpEngagementForScenario` (stays zero-touch on the hotpath). |
| `ProjectAegis.Delegation.Demo` | Console smoke over `CreateWithMvpEngagement`. |

---

## Tests (behaviour pins)

| Area | Test file |
|------|-----------|
| Core session flow | [`Orchestration/SimulationSessionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionTests.cs) |
| MVP engagement stack + order-log records | [`Orchestration/SimulationSessionMvpTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionMvpTests.cs) |
| Clock / pause / acceleration / headless bypass | [`Orchestration/SimulationSessionClockControlsTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionClockControlsTests.cs) |
| Logistics FSM order handling | [`Orchestration/SimulationSessionLogisticsFsmTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionLogisticsFsmTests.cs) |
| Planning-phase guard | [`Orchestration/SimulationSessionPhaseTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionPhaseTests.cs) |
| Replay-viewer / observer wiring | [`Orchestration/SimulationSessionObserverTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionObserverTests.cs) |
| Watch emit + auto-pause via the session | [`Orchestration/SimulationSessionWatchAttentionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchAttentionTests.cs), [`Orchestration/SimulationSessionWatchEmitTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchEmitTests.cs) |
