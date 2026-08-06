# Catalog damage / readiness / withdraw runtime — developer guide

The combat pipeline does not just decide *whether* a shot launches — it also tracks what happens
to a platform's hull afterwards. This page documents the **bounded catalog damage runtime**: the
deterministic per-tick machinery that drains platform HP, applies hit/kill/mine damage, refreshes
per-unit **readiness** and **withdraw** recommendations, and feeds one abort back into the engage
gate chain (`DamageWithdrawRecommended`).

It is an **input to and consequence of** the decision tick, not part of `DelegationOrchestrator.Tick`
itself. It runs *after* engagement resolution each tick, mutates a per-platform HP ledger, appends
`PlatformDamageChange` entries to the order log, and rebinds the withdraw trials the *next* engage
evaluation reads. Everything is pure and seeded so it never perturbs replay goldens.

Governed by **ADR-009** (combat-domain validators / bounded damage) and **req 16 / req 21 Phase B**
(readiness + platform damage model). The whole subsystem is **additive-only**: when a scenario has
no `catalogWithdraw` targets — or the catalog has no damage row for a platform — nothing here
activates and behaviour is byte-identical to the legacy Baltic fixtures.

- **Source:** damage math + appliers in
  [`src/ProjectAegis.Sim/Catalog/`](../../src/ProjectAegis.Sim/Catalog/) and
  [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/); the per-tick orchestrator in
  [`src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs`](../../src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs);
  the scenario/readiness DTOs in [`src/ProjectAegis.Sim/Scenario/`](../../src/ProjectAegis.Sim/Scenario/)
  and [`src/ProjectAegis.Sim/Policy/`](../../src/ProjectAegis.Sim/Policy/); the catalog damage columns
  in [`src/ProjectAegis.Data/Catalog/CatalogPlatformDamage.cs`](../../src/ProjectAegis.Data/Catalog/CatalogPlatformDamage.cs).
- **Related:** how to *author* the `catalogWithdraw` / `mineHazard` JSON is in
  [`scenario-policy-authoring.md`](scenario-policy-authoring.md); the engage gate that consumes the
  withdraw recommendation is in [`engagement-pipeline.md`](engagement-pipeline.md); the `Hit`/`Kill`
  outcomes this runtime reacts to come from the same pipeline; the catalog damage columns are
  seeded by the [catalog write gate](catalog-write-gate.md) / [catalog seeding](catalog-seeding.md);
  determinism rules and the golden workflow are in [`determinism-and-replay.md`](determinism-and-replay.md).

---

## Where it lives

| File | Role |
|------|------|
| [`PlatformHpLedger.cs`](../../src/ProjectAegis.Sim/Catalog/PlatformHpLedger.cs) | Mutable per-platform HP% ledger (`Ordinal`-sorted). Seeds from withdraw targets, clamps `0..100`, and folds a stable `ComputeWorldHashMix()`. |
| [`CombatDamageLevel.cs`](../../src/ProjectAegis.Sim/Catalog/CombatDamageLevel.cs) | Pure damage math: `damageLevel 0–3 = floor(severity × resilience)`, `25% HP` lost per level. |
| [`CatalogDamageHotTickApplier.cs`](../../src/ProjectAegis.Sim/Catalog/CatalogDamageHotTickApplier.cs) | The per-tick apply verbs: ambient drain, sorted hit/kill outcomes, facility slice, withdraw-trial refresh + the reason-code catalog. |
| [`MineTransitHazardHotTickApplier.cs`](../../src/ProjectAegis.Sim/Catalog/MineTransitHazardHotTickApplier.cs) | Seeded transit-mine hazard rolls (`RngDomain.MineHazard`) that damage transiting platforms. |
| [`PhaseBCatalogDamageReadinessStub.cs`](../../src/ProjectAegis.Sim/Catalog/PhaseBCatalogDamageReadinessStub.cs) | Maps `(HP%, catalog damage row)` → readiness score + withdraw recommendation. |
| [`DeterministicDamageApplyBatch.cs`](../../src/ProjectAegis.Sim/Engage/DeterministicDamageApplyBatch.cs) | Stable apply order: by `EngagementId` then `SequenceId`. |
| [`WithdrawReadinessTrialResolver.cs`](../../src/ProjectAegis.Sim/Scenario/WithdrawReadinessTrialResolver.cs) | Builds the initial sorted trial set from scenario JSON and/or catalog damage. |
| [`ReadinessPolicyEvaluator.cs`](../../src/ProjectAegis.Sim/Policy/ReadinessPolicyEvaluator.cs) | Merges scenario launch readiness + mobility + catalog withdraw trials into one `EffectiveReadiness`. |
| [`CatalogDamageWithdrawEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/CatalogDamageWithdrawEngageGate.cs) | The engage-side gate — turns a withdraw recommendation into `DamageWithdrawRecommended`. |
| [`CatalogDamageHotTickTracker.cs`](../../src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs) | The orchestrator: owns the ledger, sequences the per-tick appliers, returns changes + refreshed trials. |

The DTOs and order-log payloads it moves around:

| File | Role |
|------|------|
| [`CatalogPlatformDamage.cs`](../../src/ProjectAegis.Data/Catalog/CatalogPlatformDamage.cs) | The catalog damage row: `MaxHp`, `WithdrawThresholdPct`, `CriticalFlags`, `Resilience`. |
| [`ScenarioCatalogWithdrawTarget.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioCatalogWithdrawTarget.cs) | Per-platform seed identity `(PlatformId, CurrentHpPct = 100)`. |
| [`ScenarioWithdrawReadinessTrial.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioWithdrawReadinessTrial.cs) | Resolved trial `(PlatformId, ReadinessScore, WithdrawRecommended, CatalogResolved)`. |
| [`PlatformDamageChangeRecord.cs`](../../src/ProjectAegis.Delegation/Decision/PlatformDamageChangeRecord.cs) | `PlatformDamageChange` order-log entry `(SequenceId, SimTime, SimTick, UnitId, PreviousHpPct, NewHpPct, ReasonCode, DamageLevel)`. |

---

## Data flow (one tick)

```
DelegationOrchestrator.Tick  →  engagements resolved (Hit / Kill outcomes)
        │
        ▼   (SimulationSession.ApplyCatalogDamageHotTick, after LogEngagementResults)
CatalogDamageHotTickTracker.ApplyTick(simTick, simTime, outcomes)
        │  1. ambient drain     ApplyAmbientTickDrain     → 100 / MaxHp per tick
        │  2. combat outcomes   ApplySortedOutcomes       → sorted Hit/Kill HP deltas
        │  3. mine hazard       ApplyTransitHazardTick    → seeded RngDomain.MineHazard rolls
        │  4. refresh trials    ResolveWithdrawTrials     → new ScenarioWithdrawReadinessTrial[]
        ▼
   { Changes, WithdrawTrials, WorldHashMix }
        │
        ├─ Changes      → DecisionLog.PlatformDamageChanges (order log; C2 BDA / facility projections)
        └─ WithdrawTrials → Session.BindCatalogWithdrawTrials(...)   ← read by NEXT engage evaluation
                                    │
                                    ▼
              CatalogDamageWithdrawEngageGate.BlocksEngage(shooter, trials)
                                    │
                                    ▼
                 EngageContext.CatalogDamageWithdrawBlocked = true
                                    │
                                    ▼  (gate 7 of the engage chain)
                        EngagementAbortReason.DamageWithdrawRecommended
```

The tracker is only created when the scenario opts in — see
[`CatalogDamageHotTickTracker.TryCreate`](../../src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs):
combat domains must be enabled **and** the scenario must declare either `catalogWithdraw` targets
(`CatalogDamageHotTickApplier.IsEnabled`) or a transit `mineHazard`
(`MineTransitHazardHotTickApplier.IsEnabled`). Otherwise `TryCreate` returns `null` and the whole
runtime is a no-op.

---

## 1. The HP ledger (`PlatformHpLedger`)

The ledger is the single mutable piece of state. It is seeded once from the scenario's
`catalogWithdraw` targets via `SeedFromWithdrawTargets`, which sorts by `PlatformId` (`Ordinal`) and
clamps each `CurrentHpPct` to `0..100`. Every read/write is keyed by platform id with an
`Ordinal` comparer, and `SetHpPct` re-clamps to `0..100` — HP can never go negative or exceed full.

`GetSortedPlatformIds()` returns a stable `Ordinal`-sorted key list; **all** appliers iterate the
ledger through it so apply order is independent of dictionary insertion order.

`ComputeWorldHashMix()` folds every `(platformId, hpPct)` pair (in sorted order) into a single
`ulong` under `SimWorldHash.LayerCombatOutcome`. It is a stable, order-independent fingerprint of
the ledger used to assert determinism in tests (two runs that reach the same HP state produce the
same mix) and is surfaced on the tick result as `WorldHashMix`.

---

## 2. Damage math (`CombatDamageLevel`)

All damage — combat hits and mine hits alike — funnels through one bounded formula:

```
damageLevel = clamp( floor( clamp(severity, 0..1) × clamp(resilience, 0..2) ), 0..3 )
hpDeltaPct  = damageLevel × 25.0        // HpPctPerLevel
```

- **`severity`** — the hit severity (`DefaultHitSeverity = 1.0` for a standard combat hit; the mine's
  `Lethality` for a mine hit).
- **`resilience`** — `CatalogPlatformDamage.Resilience` (`DefaultResilience = 1.0`, clamped `0..2`).
  A tougher platform (`resilience < 1`) can absorb a hit at a lower damage level; a fragile one
  (`resilience > 1`) escalates.
- **`damageLevel`** — `0..3` (`MaxLevel = 3`). Level 0 = a glancing hit that costs **no** HP; each
  level thereafter removes a flat **25% of max HP**.

`Kill` outcomes bypass the level math and drive HP straight to `0.0`.

---

## 3. Per-tick appliers (`CatalogDamageHotTickApplier`)

The tracker calls these in a fixed order every tick; each returns a list of `DamageChange`
`(PlatformId, PreviousHpPct, NewHpPct, ReasonCode, DamageLevel)` records.

1. **Ambient drain** — `ApplyAmbientTickDrain`. A catalog-scaled attrition of one `MaxHp` unit per
   tick, expressed as `100 / MaxHp` HP%. Platforms already at `0` HP or with no catalog damage row
   are skipped, and a sub-`1e-9` delta is dropped (no spurious order-log entry). Reason code
   `CATALOG_AMBIENT_TICK`.

2. **Combat outcomes** — `ApplySortedOutcomes`. Takes the tick's queued engage outcomes, keeps only
   `Hit` and `Kill` codes, and sorts them via `DeterministicDamageApplyBatch` (by `EngagementId`,
   then `SequenceId`) so concurrent multi-domain engagements always apply in a stable order. `Kill`
   → `0.0`; `Hit` → `previousHp − CombatDamageLevel.HitHpDeltaPct(...)` (floored at `0`). Reason
   code is the outcome code itself (`Hit` / `Kill`).

3. **Facility slice** — `ApplySortedFacilityOutcomes`. A convenience filter that applies only
   outcomes whose victim id is a known facility target, reusing the same sorted-apply path (used by
   the facility-damage projection).

4. **Mine transit hazard** — see below.

5. **Withdraw-trial refresh** — `ResolveWithdrawTrials`. After all HP mutations, rebuilds the trial
   list from the *current* ledger HP for every platform via the readiness stub.

The reason codes are centralised in `PlatformDamageChangeReasonCodes`
(`CATALOG_AMBIENT_TICK`, `Hit`, `Kill`, `MINE_TRANSIT_HAZARD`).

---

## 4. Mine transit hazard (`MineTransitHazardHotTickApplier`)

The one **stochastic** source in this runtime — and therefore the one that must use seeded RNG.
It models platforms transiting a mine zone; there are no mine-laying missions or danger-area map
layers (explicitly out of scope in the ADR-009 slice).

For each transit schedule (sorted by `PlatformId`), it resolves the platform's range for the tick,
checks it is inside the hazard zone and still alive, then for each mine in range rolls
`SeededRng.UnitFloat(seed, RngDomain.MineHazard, entityId, simTick, drawIndex)`. If the draw is
below `clamp(mine.Lethality × hazard.HazardSeverity, 0..1)`, the mine hits: HP is reduced by the
`CombatDamageLevel` delta (severity = `mine.Lethality`) and a `MINE_TRANSIT_HAZARD` change is
emitted. Draws use a monotonically incrementing `drawIndex` and deterministic `entityId`
(`MineTransitHazardEntityId.FromTrial(platformId, mineId)`), and mines are iterated in
`MineId`-sorted order, so the roll sequence is fully reproducible per `(scenario, seed)`.

---

## 5. Readiness & withdraw resolution

Two DTOs describe a platform's condition:

- **`ScenarioWithdrawReadinessTrial`** — `(PlatformId, ReadinessScore, WithdrawRecommended,
  CatalogResolved)`. `CatalogResolved = false` means "no catalog damage row" → gameplay-neutral.
- **`ReadinessPolicyEvaluator.EffectiveReadiness`** — `(ReadyForLaunch, ReadinessScore,
  WithdrawRecommended, CatalogResolved)`, the merged view used by launch-readiness consumers.

**`PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness`** is the core mapping:

- No catalog damage row → `(NeutralReadinessScore = 1.0, WithdrawRecommended = false,
  CatalogResolved = false)` — the additive-only escape hatch.
- Otherwise: `currentHp = HP% / 100 × MaxHp`; **withdraw is recommended** when
  `WithdrawThresholdPct > 0 && currentHp <= WithdrawThresholdPct`.
- `ReadinessScore` = normalised HP (`HP%/100`), minus a **critical-flags penalty** of `0.1` per set
  flag (`CriticalFlags` bitmask), capped at `0.5` and floored at `0`.

**`WithdrawReadinessTrialResolver.Resolve`** builds the *initial* trial set with a clear precedence:

1. If the profile authored explicit `WithdrawReadinessTrials`, use them verbatim.
2. Else if it has `catalogWithdraw` targets, resolve each against the catalog (sorted by
   `PlatformId`); unresolved platforms fall back to a neutral, `CatalogResolved = false` trial.
3. Else return empty.

**`ReadinessPolicyEvaluator.EvaluateUnit`** is the merge point for launch readiness: it combines the
scenario `UnitReadiness` flag, catalog mobility (`PhaseBCatalogMobilityReadinessStub`), and the
catalog withdraw trial into one `EffectiveReadiness` — `ReadyForLaunch` requires *both* scenario
readiness and mobility to be ready.

---

## 6. The engage gate (`CatalogDamageWithdrawEngageGate`)

The runtime touches the engage chain in exactly one place. `SimulationSession`, when building the
`EngageContext` for a shooter, sets:

```csharp
CatalogDamageWithdrawBlocked = CatalogDamageWithdrawEngageGate.BlocksEngage(shooterUnitId, CatalogWithdrawTrials);
```

`BlocksEngage` returns `true` only when the shooter has a trial that is *both* `CatalogResolved`
**and** `WithdrawRecommended` — so a scenario without catalog damage can never block. Inside the
resolver, `CatalogDamageWithdrawEngageGate.Evaluate(in ctx)` maps that flag to
`EngagementAbortReason.DamageWithdrawRecommended` (value `20`). This is **gate 7** of the ordered
engage chain documented in [`engagement-pipeline.md`](engagement-pipeline.md) — a badly damaged unit
declines to open fire and withdraws instead.

Because trials are rebound *after* each tick's damage apply, a unit that crosses its withdraw
threshold this tick is blocked from the *next* tick onward — the ordering is intentional and stable.

---

## Determinism guarantees

This runtime is on the replay-golden hot path, so it obeys the usual
[determinism rules](determinism-and-replay.md):

- **Stable ordering everywhere** — the ledger iterates `Ordinal`-sorted platform ids; combat
  outcomes are pre-sorted by `(EngagementId, SequenceId)`; mine schedules and mines iterate in
  `Ordinal` id order.
- **Seeded RNG only** — the mine hazard is the sole random source and draws exclusively through
  `SeededRng` under `RngDomain.MineHazard` with a deterministic entity id / tick / draw index. No
  `Random.Shared`, no wall-clock.
- **No hot-path SQLite** — appliers read a gate-approved in-memory `ICatalogReader` snapshot; the DB
  is never touched during ticks (see [catalog seeding](catalog-seeding.md)).
- **Additive-only** — every branch has a neutral fall-through when a catalog damage row is missing,
  so enabling the runtime on a legacy scenario is a no-op until damage data exists.
- **Order-log driven** — HP transitions are recorded as `PlatformDamageChange` entries with a
  no-op guard (`|Δ| < 1e-9` is dropped), keeping the log free of empty transitions.

---

## Extending it without breaking goldens

- **New damage source (e.g. a new hazard)** — add an applier that mutates the ledger through
  `TryGetHpPct` / `SetHpPct` and emits `DamageChange` records with a new reason code in
  `PlatformDamageChangeReasonCodes`. Sequence it inside `CatalogDamageHotTickTracker.ApplyTick`
  *before* `ResolveWithdrawTrials`, iterate inputs in a sorted order, and — if stochastic — draw
  through `SeededRng` with a **new** `RngDomain` (never reuse `MineHazard`).
- **Tune damage severity/levels** — prefer changing catalog data (`MaxHp`, `Resilience`,
  `WithdrawThresholdPct`, `CriticalFlags`) over the constants in `CombatDamageLevel`. Gameplay
  values are data-driven; constant changes shift *every* existing golden.
- **New readiness input** — extend `ReadinessPolicyEvaluator.EvaluateUnit` and keep the neutral
  fall-through so catalog-absent scenarios are unaffected.
- **Always** re-run the full suite plus the replay golden and grep the Baltic v2 hash
  (`17144800277401907079`) before submitting — any change that alters the per-tick apply order or
  the order-log entries will move goldens.

Verification block (from [`AGENTS.md`](../../AGENTS.md)):

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
grep -r "17144800277401907079" tests/ data/
```

---

## Tests to read first

| Test | Shows |
|------|-------|
| [`CatalogDamageHotTickApplierTests`](../../src/ProjectAegis.Sim.Tests/Catalog/CatalogDamageHotTickApplierTests.cs) | Ambient drain, sorted hit/kill apply, and the `ComputeWorldHashMix` determinism assertion. |
| [`DeterministicDamageApplyBatchTests`](../../src/ProjectAegis.Sim.Tests/Engage/DeterministicDamageApplyBatchTests.cs) | The `(EngagementId, SequenceId)` apply-order contract. |
| [`MineTransitHazardHotTickApplierTests`](../../src/ProjectAegis.Sim.Tests/Catalog/MineTransitHazardHotTickApplierTests.cs) | Seeded mine rolls and zone/range gating. |
| [`PhaseBDamageCatalogConsumerTests`](../../src/ProjectAegis.Sim.Tests/Catalog/PhaseBDamageCatalogConsumerTests.cs) | Readiness score + withdraw recommendation from catalog columns. |
| [`CatalogDamageWithdrawEngageGateTests`](../../src/ProjectAegis.Sim.Tests/Engage/CatalogDamageWithdrawEngageGateTests.cs) / [`MvpEngagementDamageWithdrawTests`](../../src/ProjectAegis.Sim.Tests/Engage/MvpEngagementDamageWithdrawTests.cs) | The gate mapping to `DamageWithdrawRecommended`, end-to-end. |
| [`CatalogDamageHotTickEngageTests`](../../src/ProjectAegis.Delegation.Tests/Sim/CatalogDamageHotTickEngageTests.cs) / [`CatalogDamageWithdrawEngageTests`](../../src/ProjectAegis.Delegation.Tests/Sim/CatalogDamageWithdrawEngageTests.cs) | The tracker wired through `SimulationSession` (order-log records + the abort). |
