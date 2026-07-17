# Engagement / kill-chain pipeline — developer guide

`MvpEngagementResolver` is the **single place an `Engage` order becomes a launch or an abort**.
It takes an `EngageRequest` plus a per-shot `EngageContext`, runs an ordered chain of
fire-control gates (policy/ROE → domain → readiness → EMCON → track → magazine → envelope →
DLZ), consumes ammo, and folds a deterministic hit/intercept/kill outcome. It is the tick-8
seam behind the delegation framework, the Baltic replay harness, the QA Gauntlet, and the
combat-outcome replay goldens.

- **Source:** [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) —
  entry point [`MvpEngagementResolver.cs`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs).
- **Related:** the *concepts* (world-state hashing, seeded RNG, golden workflow) live in
  [`determinism-and-replay.md`](determinism-and-replay.md); the stable machine-readable
  **abort codes** the resolver emits are documented in
  [`abort-reason-catalog.md`](abort-reason-catalog.md); the *end-to-end runner* that drives this
  pipeline headlessly is [`baltic-replay-harness.md`](baltic-replay-harness.md). This page
  documents what `Resolve` actually **does**, in what order, and how to extend it safely.

> **Why one resolver.** Every headless and Unity path that resolves a shot funnels through
> `IEngagementResolver.Resolve`, so the gate order, the RNG draw layout, and the world-hash
> contribution are defined in exactly one place. New gates and weapon behaviours are added
> *inside* the chain, not by forking it — that is what keeps the combat-outcome goldens and the
> gauntlet oracle stable.

---

## The `Resolve` seam

```csharp
public interface IEngagementResolver
{
    EngageResult Resolve(in EngageRequest request);
}
```

`MvpEngagementResolver` is the production implementation; `StubEngagementResolver` and
`RecordingEngagementResolver` are test/fixture doubles that satisfy the same contract for
isolation. The resolver is stored by [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs)
and invoked once per queued request during a tick:

```text
SimTickPipeline.EnqueueEngagement(request)   // one per accepted Engage order this tick
SimTickPipeline.TickOnce(mode)
  └─ foreach pending request: _engagement.Resolve(request)   → EngageResult
     RecomputeWorldHash: SimWorldHash.Combine(core, detection, engageMix, killMix)
```

`engageMix` folds each `EngageResult`'s `EngagementId` + first `OutcomeCode` char; `killMix` is
`MvpEngagementResolver.KilledTargets.MixHash()`. Both are pinned by the replay goldens, so any
change to gate order, outcome codes, or magazine sizing shifts the golden hash — see
[Extending the pipeline](#extending-the-pipeline).

### Inputs and outputs

```csharp
readonly record struct EngageRequest(
    ulong ShooterUnitId, ulong TargetId, ulong MountId, ulong SimTick);

readonly record struct EngageResult(
    bool   Launched,
    ulong  EngagementId = 0,
    EngagementAbortReason AbortReason = EngagementAbortReason.None,
    string? OutcomeCode = null,     // "Hit" | "Miss" | "Intercept" | "Kill" on launch
    double PkDraw = 0);
```

- `EngageResult.Launch(id)` — a shot went out; `EngagementId` is a monotonic counter
  (`_nextEngagementId++`, starting at `1`) unique within the resolver instance.
- `EngageResult.Aborted(reason)` — no shot; `AbortReason` is one of the
  [`EngagementAbortReason`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs) enum
  values, turned into a stable order-log code (`Engagement|…|<CODE>`) by
  [`EngagementAbortReasonCodes.ToLogCode`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReasonCodes.cs).

The **per-shot facts** the gates read come from `EngageContext` — a wide
`readonly record struct` supplied by the world query (`IEngageWorldQuery.TryGetContext`). Its
fields are the resolver's whole input surface (defaults shown are the record defaults):

| Field | Purpose |
|-------|---------|
| `RangeMeters` | Shooter→target range, checked against the envelope and DLZ. |
| `Envelope` (`WeaponEnvelope(Min, Max)`) | Min/max launch range; `Contains(range)` is the hard envelope gate. |
| `RoundsRemaining`, `SalvoSize` | Pre-check ammo hint + rounds consumed per launch (min 1). |
| `HasFireControlTrack`, `ContactIdentified`, `MountOnline` | Fire-control / sensor / mount readiness. |
| `RadarEmconActive` | `false` ⇒ `EmconOff` abort (can't illuminate under EMCON). |
| `TrackSpoofed` | `true` ⇒ `TrackSpoofed` abort (cyber/EW). |
| `AirOperationsReady` | `false` ⇒ `AirNotReady` abort. |
| `CombatDomain` | Selects the domain validator (`Air`…`Facility`). |
| `*AspectInEnvelope` (6 flags) | Per-domain aspect gate for the ADR-009 validators. |
| `IsHypersonicTarget`, `HasHypersonicDefenseLayer` | Hypersonic defence-layer gate (req 09). |
| `WeaponTechnologyLevel`, `WeaponRequiresBlackProject` | Speculative TL / black-project gate (req 10). |
| `CatalogDamageWithdrawBlocked` | Catalog damage-trial withdraw recommendation (ADR-009). |
| `DlzPersonality` (`Normal`/`Early`/`Late`) | Timing bias inside the DLZ band. |
| `PkBase`, `PkIntercept`, `PkKill` | Combat-outcome probabilities (see [Combat outcome](#combat-outcome-resolution)). |

---

## The gate chain (exact order)

`Resolve` runs the following checks **top to bottom** and returns the *first* abort it hits.
This order is a hard contract — it is what the goldens encode. The two `combatDomains*`-gated
and `policyEvaluator`-gated steps only run when their dependency is wired.

| # | Gate | Abort reason on failure |
|---|------|-------------------------|
| 1 | Target already killed (`KilledTargetRegistry.IsKilled`) | `TargetDestroyed` |
| 2 | World query resolves an `EngageContext` (`IEngageWorldQuery.TryGetContext`) | `NoFireControlTrack` |
| 3 | [`SpeculativeEngageGate`](../../src/ProjectAegis.Sim/Scenario/SpeculativeEngageGate.cs) — TL cap / black-project mode (req 10) | `TechnologyLevelExceeded`, `BlackProjectRequired` |
| 4 | Policy evaluator *(if injected)* — resolves live per-unit `EffectivePolicy`, evaluates `FireGuided` | mapped from `FireAbortReason`: `RoeHoldFire`, `WeaponsTight`, `WraSalvo`, `OutOfEnvelope` (WRA range), `EmconOff`, `NoFireControlTrack` |
| 5 | Combat-domain aspect validators *(if `combatDomainsEnabled`)* — [`DomainValidatorRegistry.Validate`](../../src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs) (ADR-009) | `AirAspectBlock` … `FacilityAspectBlock`, else `DomainNoSolution` |
| 6 | Air operations ready | `AirNotReady` |
| 7 | [`CatalogDamageWithdrawEngageGate`](../../src/ProjectAegis.Sim/Engage/CatalogDamageWithdrawEngageGate.cs) (ADR-009, additive-only) | `DamageWithdrawRecommended` |
| 8 | Track not spoofed | `TrackSpoofed` |
| 9 | Radar/EMCON active | `EmconOff` |
| 10 | Has fire-control track | `NoFireControlTrack` |
| 11 | Ammo pre-check (`RoundsRemaining` **or** ledger rounds > 0) | `MagazineEmpty` |
| 12 | [`CombatDomainValidator`](../../src/ProjectAegis.Sim/Engage/CombatDomainValidator.cs) — legacy per-domain gate (req 18): mount online, subsurface solution, land half-range | `MountOffline`, `DomainNoSolution`, `OutOfEnvelope` |
| 13 | [`HypersonicEngageGate`](../../src/ProjectAegis.Sim/Engage/HypersonicEngageGate.cs) (req 09) | `DomainNoSolution` |
| 14 | Weapon envelope (`Envelope.Contains(RangeMeters)`) | `OutOfEnvelope` |
| 15 | [`DlzEngageGate.AllowsLaunch`](../../src/ProjectAegis.Sim/Engage/DlzEngageGate.cs) — dynamic launch zone + personality | `DlzOut` |
| 16 | **Consume salvo** (`MagazineLedger.TryConsumeSalvo`) | `MagazineEmpty` |
| 17 | **Launch** → [combat outcome](#combat-outcome-resolution) | — |

> **State mutation is last.** The only side effect on the ammo ledger is step 16, *after* every
> gate has passed. An abort never consumes rounds, so re-running a scenario or reordering
> engagements at the session layer cannot silently drain magazines.

### Two-layer ROE / domain gating

There are deliberately **two** domain checks and **two** ROE-ish surfaces:

- **Policy (step 4)** is the ROE/WRA boundary via [`IPolicyEvaluator`](../../src/ProjectAegis.Sim/Policy/IPolicyEvaluator.cs)
  (ADR-002). The resolver resolves the shooter's live `EffectivePolicy` through its own
  `_resolvePolicy` callback and passes it in a `PolicyContext` tagged with a non-zero
  `ResolvedPolicySnapshotMarker` so `PolicyEvaluator` **trusts the resolved value instead of
  re-resolving** — this is what lets a unit's own `HoldFire`/`WeaponsTight` ROE win even when it
  differs from the evaluator's default. `MapPolicyDenial` translates the sim `FireAbortReason`
  into the `Engage`-family abort reason.
- **Domain validators** split by intent: the ADR-009 `DomainValidatorRegistry` (step 5, aspect
  envelopes, opt-in via `combatDomainsEnabled`) and the always-on legacy `CombatDomainValidator`
  (step 12, mount/subsurface/land specifics). The registry iterates validators in **stable
  ordinal `CombatDomain` order** (`Air`=0 … `Facility`=5) and only runs the one matching
  `ctx.CombatDomain`.

### Dynamic launch zone (DLZ)

[`DlzEvaluator`](../../src/ProjectAegis.Sim/Engage/DlzEvaluator.cs) classifies range into
`OutOfZone` / `Approaching` / `InZone` relative to the envelope (`< Min×0.9` or `> Max`
⇒ out; `> Max×0.85` ⇒ approaching; else in-zone). `DlzEngageGate` then applies a personality:

| `DlzPersonality` | Launches when |
|------------------|---------------|
| `Normal` (default) | `InZone` |
| `Early` | `InZone` **or** `Approaching` |
| `Late` | `InZone` **and** `range ≤ Max×0.7` |

Personality changes *timing only*, never the underlying zone math (req 14).

---

## Combat outcome resolution

On launch, [`CombatOutcomeResolver`](../../src/ProjectAegis.Sim/Engage/CombatOutcomeResolver.cs)
folds the outcome with **three deterministic draws** from the `Combat` RNG domain, all keyed by
`(EngagementId, SimTick)` and separated only by `drawIndex`:

```text
Apply(pkBase)             draw 0  → "Hit" if draw < PkBase else "Miss"
ApplyInterceptOnHit(pkIntercept)  draw 1  → downgrade "Hit" → "Intercept" if PkIntercept > 0 and draw < it
ApplyKillOnHit(pkKill)    draw 2  → promote remaining "Hit" → "Kill" if draw < PkKill
```

Draws use the **stateless, coordinate-addressed** `SeededRng.UnitFloat(seed, RngDomain.Combat,
engagementId, simTick, drawIndex)`, so a given engagement's outcome does not depend on how many
other shots resolved first — see the
[determinism guide → seeded RNG](determinism-and-replay.md#seeded-rng). Intercept runs before
kill, so an intercepted hit is never also promoted to a kill. `PkDraw` carries draw 0 for AAR /
explainability. Probabilities are `Math.Clamp`ed to `[0,1]`; `PkIntercept = 0` (the default)
short-circuits the intercept draw entirely.

### Kill registry and world hash

[`KilledTargetRegistry`](../../src/ProjectAegis.Sim/Engage/KilledTargetRegistry.cs) tracks
destroyed target ids. It gates step 1 (`TargetDestroyed`) so a dead target is never re-engaged,
and its `MixHash()` folds the sorted kill ids into the `killMix` layer of the world hash. **The
resolver does not mark kills itself** — `SimulationSession.LogEngagementResults` inspects each
`Kill` outcome and calls `MarkKilled`, keeping the resolver a pure function of its inputs.

---

## How the session drives it

Under the delegation framework, [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
turns executed `Engage` orders into requests each tick:

```text
Orchestrator.Tick(state)              → ExecutedOrders (agent decisions, ROE-gated)
  filter Kind == Engage
  comms gate (CommsStateProjection.BlocksNewEngagement) → PolicyDenial COMMS_DENIED
  SwarmSalvoDeconfliction.Allocate     → one shooter per target this tick (deterministic sort)
  per accepted order:
    PrimeEngageWorld(request, ...)     → populate the IEngageWorldQuery context
    Sim.EnqueueEngagement(request)
  Sim.TickOnce(RealTime)               → MvpEngagementResolver.Resolve per request (tick 8)
  LogEngagementResults / MarkKilled    → order-log rows + kill registry
  SurfaceRoePolicyDeniedEngagements    → emit ROE_WEAPONS_TIGHT abort rows for policy-denied intents
  ApplyCatalogDamageHotTick            → catalog damage/HP fold
```

[`SwarmSalvoDeconfliction`](../../src/ProjectAegis.Sim/Engage/SwarmSalvoDeconfliction.cs)
allocates by sorted `(shooterId, targetId, weaponId)` so at most one shooter engages each target
per slot — deterministically, regardless of order-log iteration order (req 14).

Build the MVP session for a scenario with
`SimulationSession.BindMvpEngagementForScenario(orchestrator, scenarioPolicyId, catalog)`; it
resolves the engage defaults (envelope, Pk, magazine rounds) from the scenario policy JSON's
`engage` block (or the MVP fallback) and applies catalog weapon envelopes via
`CatalogEngageEnvelope.Apply`. Magazine capacity is seeded once per shooter+mount with
`MagazineLedger.EnsureInitialRounds` (no refill after consumption).

---

## Determinism rules (engage path)

- **No ambient randomness.** Combat draws come only from `SeededRng.UnitFloat` in the `Combat`
  domain; never `Random.Shared`, `DateTime.UtcNow`, or `Guid.NewGuid()`.
- **Gate order is fixed** and encoded in the goldens — never reorder, insert, or remove a gate
  without regenerating the combat-outcome goldens (below).
- **`EngagementId` is per-instance monotonic** — do not seed it from wall-clock or a shared
  static.
- **Iteration must be ordered** — the domain-validator registry and swarm deconfliction sort
  their inputs precisely so no `Dictionary`/`HashSet` enumeration order leaks into the hash.
- **Aborts are side-effect free** — only step 16 mutates the magazine ledger.

---

## Extending the pipeline

| Task | How | Golden impact |
|------|-----|---------------|
| **Add a new abort reason** | Append to the `EngagementAbortReason` enum, add its `Engage` mapping in the [abort-reason manifest](abort-reason-catalog.md), and place the gate at the correct point in `Resolve`. | Shifts combat-outcome goldens **only if** the gate can fire in a golden scenario. |
| **Add a domain validator** | Implement `IDomainValidator` for a `CombatDomain`, register it in `DomainValidatorRegistry` (kept in ordinal order). Runs only when `combatDomainsEnabled`. | Neutral unless it denies a golden shot. |
| **Add a new outcome code** | Extend `EngagementOutcomeCodes` and a `CombatOutcomeResolver` step with a **new `drawIndex`** (do not reuse 0/1/2). | Changes `engageMix` → regenerate goldens. |
| **Change Pk / envelope / magazine defaults** | Edit `data/scenarios/*.policy.json` `engage` block (gameplay numbers are data, not C# constants). | Changes outcomes → regenerate the affected scenario's golden. |
| **Add a new weapon/platform** | Extend the catalog via the [write gate](catalog-write-gate.md); envelopes flow in through `CatalogEngageEnvelope.Apply`. | Neutral unless it enters a golden scenario. |

Regenerate goldens through the [Baltic replay harness](baltic-replay-harness.md) /
`ProjectAegis.Delegation.Demo`, then run the full verification block in
[`AGENTS.md`](../../AGENTS.md#build--test-commands) (`dotnet build`, `dotnet test`, PlayMode
smoke) and confirm the Baltic v2 replay hash invariant still holds.

---

## Consumers & tests

| Consumer | Uses |
|----------|------|
| [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | Headless delegation path — enqueue/resolve, kill marking, comms/ROE surfacing |
| [`BalticReplayHarness`](baltic-replay-harness.md) | End-to-end runner behind replay golden + QA Gauntlet |
| [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs) | Tick-8 resolve seam + `engageMix`/`killMix` hash layers |
| Mission Editor CLI kill-chain report | `MvpEngagementResolver` outcomes surfaced per shot |

Tests live under `src/ProjectAegis.Sim.Tests/Engage/` (resolver gate order, DLZ, magazine,
domain validators, combat outcome) and are part of the ≥1638-test solution baseline; the
combat-outcome reproducibility goldens live in [`tests/regression/`](../../tests/regression/).

## See also

| Topic | Doc |
|-------|-----|
| Determinism rules, hashing, seeded RNG | [determinism-and-replay.md](determinism-and-replay.md) |
| Stable abort codes (`Engagement|…|<CODE>`) | [abort-reason-catalog.md](abort-reason-catalog.md) |
| End-to-end headless runner | [baltic-replay-harness.md](baltic-replay-harness.md) |
| Policy evaluator boundary | [`adr-002-policy-evaluator.md`](../architecture/adr-002-policy-evaluator.md) |
| Combat domain validators | [`adr-009-combat-domain-validators.md`](../architecture/adr-009-combat-domain-validators.md) |
| Tick pipeline order + world-hash layers | [`adr-004-tick-pipeline-order.md`](../architecture/adr-004-tick-pipeline-order.md) |
| Sim assembly overview | [`../../src/ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md) |
