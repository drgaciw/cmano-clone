# Simulation session orchestration — developer guide

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) is the
**conductor** that runs one tick end-to-end: it drives the delegation decision tick, then the sim
engagement phase, then the per-tick side systems (catalog damage, BDA lifecycle, logistics FSMs,
balance drift), and folds everything back into the order log. Almost every subsystem documented
elsewhere in this folder is *invoked from here* in a fixed order — this page is the map of that
order.

- **Source:**
  [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  (`Orchestration/`). The delegation half it wraps is
  [`DelegationOrchestrator`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs);
  the sim half is [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs).
- **Consumers:** the [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
  holds a `Session` and calls `Session.Tick(observed)` on its hotpath; the
  [`BalticReplayHarness`](baltic-replay-harness.md) drives the bridge (and thus the session) for
  headless golden/gauntlet runs.
- **Related deep-dives (all called from the tick chain below):**
  [`agent-decision-pipeline.md`](agent-decision-pipeline.md) (the delegation `Tick`),
  [`engagement-pipeline.md`](engagement-pipeline.md) (the shot resolver),
  [`comms-degradation-runtime.md`](comms-degradation-runtime.md) (the engage block),
  [`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md) (hot-tick damage),
  [`logistics-fuel-runtime.md`](logistics-fuel-runtime.md) (bingo gate),
  [`watch-attention-auto-pause-runtime.md`](watch-attention-auto-pause-runtime.md) (pause/watch),
  [`balance-drift-telemetry.md`](balance-drift-telemetry.md) (advisory outcome recording).

> **Boundary.** `SimulationSession` lives in the **engine-agnostic** `ProjectAegis.Delegation`
> assembly (ADR-010 §2–3): it holds no Unity types and never renders. The `DelegationBridge`
> hotpath that calls it is **zero-touch through Release v1** — extend behaviour *inside* the
> session's private helpers, not by editing the bridge. Everything the session does is deterministic
> given `(scenario, seed, ObservedState stream)`; the fingerprinted authority is the order log.

---

## Construction & binding

A session pairs one `DelegationOrchestrator` with one `SimTickPipeline`. The bare constructor uses a
[`StubEngagementResolver`](../../src/ProjectAegis.Sim/Engage/) (no real combat); the **`BindMvp*`**
factories wire the production [`MvpEngagementResolver`](engagement-pipeline.md) plus the side-system
state.

| Factory | Use |
|---------|-----|
| `new SimulationSession(globalSeed, engagement?, policyEvaluator?)` | Minimal; stub resolver unless one is passed. |
| `BindMvpEngagement(orchestrator, defaultEngageContext, defaultMagazineRounds = 2, catalogReader?)` | Full engagement bind: builds the `MvpEngagementResolver` (world query, `MagazineLedger`, `KilledTargetRegistry`, speculative-TL + engage-defaults from the scenario policy, combat-domains flag), the `BalanceDriftAdvisoryConsumer`, the `CatalogDamageHotTickTracker`, and the `BdaContactLifecycleRegistry`. |
| `BindMvpEngagementForScenario(orchestrator, scenarioPolicyId, catalog?, weaponId)` | Resolves the `ScenarioPolicyProfile` → `EngageContext` (`ResolveEngageContext()` or `MvpFallback`), applies the [`CatalogEngageEnvelope`](../../src/ProjectAegis.Sim/Catalog/) for the weapon, then delegates to `BindMvpEngagement`. This is what `DelegationBridge` calls. |
| `CreateWithMvpEngagement(globalSeed[, ctx, rounds, policy])` | Test/demo convenience over `BindMvpEngagement` with a Baltic-patrol fixture context. |

The heavy collaborators are exposed as `init`-only properties (`EngageWorld`, `Magazines`,
`KilledTargets`, `MvpResolver`, `CatalogReader`, `CatalogDamageHotTickTracker`,
`BdaContactLifecycleRegistry`, `BalanceDriftConsumer`). A few are settable to inject optional
behaviour per run: `FuelTimeline`, `UnitReadiness`, `AirOps`, `BoatOps`, `NextEngageSalvoOverride`
(one-shot), and `IsContactSpoofed`.

---

## Phase gate

`Phase` mirrors `Orchestrator.Phase` — [`SimulationPhase`](../../src/ProjectAegis.Delegation/Orchestration/SimulationPhase.cs)
is `Planning` or `Executing`. `BeginExecution()` transitions to `Executing`. Both tick entry points
**return `false` and do nothing while `Planning`**:

- `Tick(state)` — interactive/real-time path (`headlessOverride: false`).
- `TickHeadless(state)` — **S115 headless/CI path**: advances the engagement pipeline *even when the
  clock is paused*, using `TimeCompressionMode.HeadlessBatch`. The pause flag and watch reason are
  preserved so an interactive resume still works after a batch. This is the never-stall path that
  keeps replay/gauntlet runs deterministic regardless of watch auto-pause.

---

## The `RunExecutingTick` gate chain

Both entry points funnel into one private method. The order is fixed and load-bearing:

```text
RunExecutingTick(state, headlessOverride):
 1. Orchestrator.Tick(state)                     → delegation decision tick (see agent-decision-pipeline)
 2. simTick = max(0, (long)state.SimTime)
 3. if Clock.IsPaused && !headlessOverride:
        SurfaceRoePolicyDeniedEngagements; return  → paused: only surface ROE denials, no engagement
 4. collect ExecutedOrders where Kind == Engage
 5. commsBlocksEngage = CommsStateProjection.BlocksNewEngagement(Project(DecisionLog).State)  (Denied)
 6. deconflict: per order → ResolveEngageVictim → SwarmSalvoDeconfliction.Slot(shooter, victim)
        acceptedPairs = SwarmSalvoDeconfliction.Allocate(slots)   → ≤1 shooter per victim
 7. foreach engage order:
        if commsBlocksEngage: append PolicyDenialRecord(CommsDenied); continue
        resolve victim; skip if (shooter,victim) not in acceptedPairs
        PrimeEngageWorld(request, state, shooter); Sim.EnqueueEngagement(request); queue it
 8. Sim.TickOnce(mode)                            → MvpEngagementResolver resolves each queued shot
 9. LogEngagementResults(state, queued)           → order-log rows (engagement / magazine / outcome / kill)
10. SurfaceRoePolicyDeniedEngagements(state, simTick)
11. ApplyCatalogDamageHotTick(state, queued)      → PlatformDamageChange + BDA lifecycle + withdraw trials
12. extraSteps = AccelerationFactor - 1 → Sim.TickOnce(mode) each   (time acceleration)
13. AdvanceLogisticsFsms(state)                   → aircraft/boat launch/recover FSMs + fuel FSM ticks
```

`mode` is `HeadlessBatch` when `headlessOverride`, else `RealTime`. Steps 5–7 are where **comms
denial** and **swarm salvo deconfliction** gate which orders even reach the resolver; the resolver's
own 17-gate chain then decides launch-vs-abort ([engagement-pipeline.md](engagement-pipeline.md)).

### Victim resolution

`ResolveEngageVictim(order, state)` maps a *shooter* order to the *victim* it actually fires at
(the `Engage` order names the acting unit, not the target). Baltic-v3 dual-side logic:

1. **Red-force shooter** → a blue victim (`state.PrimaryBlueForceContactId`, else the registry
   default, else `"u1"`).
2. Otherwise a per-shooter preference (`state.PreferredHostileByShooter[shooter]`) when present —
   this is what enables multi-domain concurrent engagements.
3. Otherwise `state.PrimaryHostileContactId` (else the default red unit, else `"hostile-1"`).

### Priming the engage world

`PrimeEngageWorld` builds the per-shot [`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs)
by folding *live* `ObservedState` + scenario/catalog gates onto the default template: fire-control
track, radar EMCON (∧ [`ScenarioEmconResolver`](../../src/ProjectAegis.Sim/Sensors/)), air-ops
readiness (∧ catalog mobility stub), catalog damage-withdraw block, **fuel bingo block**
(`FuelTimeline.IsBingo`), track-spoof (`IsContactSpoofed`), salvo size (honouring the one-shot
`NextEngageSalvoOverride`), and the shotgun-rounds threshold. It also seeds the magazine ledger from
the catalog and caps it to the policy round count. Everything here is read-then-set — no mutation of
sim state.

---

## What lands in the order log

The session is the place where sim outcomes become **fingerprinted order-log rows** (via
[`OrderLogEntryFactories`](../../src/ProjectAegis.Delegation/)):

| Helper | Rows appended |
|--------|---------------|
| `LogEngagementResults` | `EngagementRecord` (launched or abort code) per queued shot; on launch also a `MagazineChangeRecord` (`-salvo`, `Fire`), an ordnance-band `OrdnanceStateChangeRecord` (on band change), an `EngagementOutcomeRecord` (outcome code + `PkDraw`), a `KilledTargets.MarkKilled` on `Kill`, and a `BalanceDriftConsumer.RecordEngagementOutcome` (advisory). Missing result → `NoResult`. |
| `SurfaceRoePolicyDeniedEngagements` | Scans the decision log for this tick's `PolicyDenialRecord`s with `AttemptedKind == Engage` and `Reason == WeaponsTight`, and emits a `WeaponsTight` engagement row per denied shooter — the bridge that makes ROE denials visible in the picture. Runs on **both** the paused and executing paths. |
| `ApplyCatalogDamageHotTick` | Per launched `Hit`/`Kill`, builds a `CatalogDamageHotTickApplier.OutcomeApply` (Hit → `CombatDamageLevel.DefaultHitSeverity`), runs `CatalogDamageHotTickTracker.ApplyTick`, appends each `PlatformDamageChangeRecord`, rebinds withdraw trials (`BindCatalogWithdrawTrials`), and drives the BDA contact lifecycle. |
| `ApplyBdaContactLifecycleHotTick` | For each newly-lost target it calls `BdaContactLifecycleRegistry.MarkLost` and, for **own-side** losses only, `ReportOwnSideLoss(...)` → a watch-attention event (hostile losses stay silent here). |

---

## Clock, acceleration & the watch/auto-pause seam

The session owns the human-facing pause/watch behaviour on top of the sim clock:

- **Pause/resume:** `IsSimPaused`, `PauseSim()`, `ResumeSim()`. `TryResumeSim(explicitOverride)`
  refuses to resume while the [`WatchAutoPauseGate`](watch-attention-auto-pause-runtime.md) reports
  unresolved pause-class attention (unless `explicitOverride` is set), then clears the reason.
- **Acceleration:** `SetTimeAccelerationFactor(n)` / `TimeAccelerationFactor`; step 12 above runs
  `n − 1` extra `Sim.TickOnce` calls so a single logical tick advances the sim `n` steps.
- **Watch ingestion (S115/S116):** `ReportWatchAttention(evt)` enqueues to `WatchQueue` and
  auto-pauses when `WatchPauseGate.ShouldAutoPause(evt)`. `ReportContactTransitions(...)` and
  `ReportOwnSideLoss(...)` are pure factory adapters over
  [`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/) — first hostile/unknown
  contact and own-side loss become watch events with idempotent EventIds, called from the
  harness/sensor path **without** touching `DelegationBridge`.

The watch state is session-local and stays **off the fingerprinted order log**, so pausing/resuming
never changes a replay hash — see [`watch-attention-auto-pause-runtime.md`](watch-attention-auto-pause-runtime.md).

---

## Logistics FSMs

`AdvanceLogisticsFsms` runs after engagement each tick. `ProcessLogisticsOrders` maps
executed order kinds to state-machine calls — `LaunchAircraft` / `AbortLaunchAircraft` on the
[`AirOpsStateMap`](../../src/ProjectAegis.Delegation/Logistics/) (lazily created; a unit is seeded
`OnGround` with catalog/`UnitReadiness` launch readiness) and `LaunchBoat` / `RecoverBoat` /
`AbortBoatLaunch` on the `BoatOpsStateMap` — then ticks both maps and any `FuelTimeline` by one step.

---

## Determinism rules

1. **Seeded everywhere.** The resolver and clock derive from `SimSeed.FromScenario(globalSeed)`;
   there is no `DateTime.UtcNow` / `Random.Shared` on the tick path.
2. **Ordered iteration.** Engage orders, deconfliction slots, and damage outcomes are processed in
   list order; deconfliction sorts by `(shooterId, targetId, weaponId)` so the accepted set is
   independent of enumeration order.
3. **The order log is authority.** Sim state is transient; only the appended records are
   fingerprinted, so new behaviour must land as deterministic order-log rows to be replay-visible.
4. **Paused vs. headless.** A real-time paused tick emits *only* ROE-denial surfacing;
   `TickHeadless` ignores the pause to keep CI/replay progressing. Do not add engagement side
   effects to the paused branch.

---

## Extending it safely

- **Add a per-tick side system** by inserting an ordered step in `RunExecutingTick` *after*
  `Sim.TickOnce` and before/after the existing hot-tick appliers — mirror the null-guarded
  `if (Tracker == null) return;` pattern so unbound runs are unaffected, and append any new state as
  order-log rows (this is what keeps the Baltic v2 golden `17144800277401907079` and the gauntlet
  oracle stable).
- **Add an engage gate** by extending `PrimeEngageWorld`'s `EngageContext` fold or the
  resolver's chain ([engagement-pipeline.md](engagement-pipeline.md)) — not by pre-filtering orders
  ad hoc, so comms/deconfliction/ROE ordering stays intact.
- **Never edit `DelegationBridge.cs`** to change session behaviour (zero-touch invariant); the bridge
  only *calls* `Session.Tick`.
- **Verify** with the headless suite and the C2 proxy Play Mode smoke (per
  [`AGENTS.md`](../../AGENTS.md#build--test-commands)); the session is exercised by the
  `SimulationSession*` tests and the `BalticReplayHarness` golden/gauntlet runs.
