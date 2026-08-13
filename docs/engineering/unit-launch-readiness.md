# Unit launch readiness — developer guide

**Launch readiness** answers one runtime question per shooter: *is this unit allowed to launch a
weapon this tick?* (req 16). It is a small, deterministic seam that starts as scenario metadata and
ends as the `AirOperationsReady` input to the engage kill chain — where `false` becomes an
`AirNotReady` abort.

This is **distinct from** the catalog *damage* readiness runtime
([catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)), which tracks HP% and
withdraw thresholds. The two are merged only inside `ReadinessPolicyEvaluator` (below).

- **Source:** the map + factory are pure C# in
  [`ProjectAegis.Delegation/Sim/`](../../src/ProjectAegis.Delegation/Sim/) and
  [`ProjectAegis.Data/Scenario/`](../../src/ProjectAegis.Data/Scenario/); the merge policy is in
  [`ProjectAegis.Sim/Policy/`](../../src/ProjectAegis.Sim/Policy/).
- **Related:** how the readiness flag becomes an abort is in
  [engagement-pipeline.md](engagement-pipeline.md) (gate 6, `AirNotReady`); the air-ops FSM that a
  ready airframe feeds is in [deck-operations-runtime.md](deck-operations-runtime.md).

---

## The flow (authoring → engage gate)

```
scenario metadata.unitReadiness[unitId].readyForLaunch   (authoring, default true)
  │  ScenarioMetadataDto.UnitReadiness  →  ScenarioPolicyProfile.UnitReadiness
  ▼
UnitReadinessMapFactory.FromMetadata(metadata)   → IReadOnlyDictionary<string,bool>? (null if none)
  ▼
new UnitReadinessMap(readyByUnitId)              (Delegation/Sim)
  ▼
SimulationSession.UnitReadiness  (set by DelegationBridge / BalticReplayHarness)
  ▼
per shot: UnitReadiness?.IsReadyForLaunch(shooterUnitId) ?? true  →  EngageContext.AirOperationsReady
  ▼
MvpEngagementResolver gate 6: !AirOperationsReady ⇒ EngagementAbortReason.AirNotReady
```

---

## Where it lives

| File | Role |
|------|------|
| [`ScenarioMetadataDto.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/ScenarioMetadataDto.cs) | Authoring input: `UnitReadiness` = map of unit id → `ScenarioUnitReadinessDto { ReadyForLaunch = true }` (req 16). |
| [`UnitReadinessMapFactory.cs`](../../src/ProjectAegis.Data/Scenario/UnitReadinessMapFactory.cs) | `FromMetadata(metadata)` → `IReadOnlyDictionary<string,bool>?` (returns **null** when no readiness is authored). |
| [`UnitReadinessMap.cs`](../../src/ProjectAegis.Delegation/Sim/UnitReadinessMap.cs) | The runtime map: `IsReadyForLaunch(unitId)` (**defaults `true`** for untracked ids) and `IsTracked(unitId)`. Ordinal-keyed, copied at construction. |
| [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | Holds `UnitReadiness` and reads `IsReadyForLaunch(shooterUnitId)` per shot to set `AirOperationsReady`. |
| [`ReadinessPolicyEvaluator.cs`](../../src/ProjectAegis.Sim/Policy/ReadinessPolicyEvaluator.cs) | Req-16/21 merge: combines scenario launch readiness with catalog mobility + withdraw trials into `EffectiveReadiness`. |
| [`EngageContext.cs`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) | Carries `AirOperationsReady` (default `true`) into the resolver. |

---

## `UnitReadinessMap` — default-ready semantics

```csharp
public bool IsReadyForLaunch(string unitId) =>
    !_readyByUnitId.TryGetValue(unitId, out var ready) || ready;   // untracked ⇒ ready
```

The map is **permissive by omission**: a unit id with no explicit entry is treated as ready, so
scenarios only need to list the airframes they want to *hold down*. `IsTracked(unitId)` distinguishes
"explicitly present (a tracked airframe)" from "defaulted". Keys are compared `Ordinal` and copied
into a private dictionary at construction, so the map is immutable and deterministic.

## Who sets `SimulationSession.UnitReadiness`

Two composition points build the map from `profile.UnitReadiness` (or an explicit override) — both
read-only consumers of the seam, not part of the sim hot loop:

- [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs):
  when the active `ScenarioPolicyProfile` has any readiness entries, sets
  `Session.UnitReadiness = new UnitReadinessMap(profile.UnitReadiness)` and reads
  `IsReadyForLaunch(shooterUnitId)` into `EngageContext.AirOperationsReady`.
- [`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs):
  uses `unitReadiness ?? profile.UnitReadiness` so headless runs (and the sample CLI) can inject a
  readiness map directly.

## `ReadinessPolicyEvaluator` — merging launch readiness with catalog withdraw

`EvaluateUnit(platformId, profile, catalog)` (req 16/21) is **additive-only**: absent catalog data
leaves scenario readiness unchanged. It computes:

```csharp
scenarioReady   = profile.UnitReadiness.TryGetValue(platformId, …) ? ready : true;  // default true
readyForLaunch  = scenarioReady && mobility.ReadyForLaunch;                          // ∧ catalog mobility
```

then folds in the catalog withdraw trial (if any) to return an `EffectiveReadiness(ReadyForLaunch,
ReadinessScore, WithdrawRecommended, CatalogResolved)`. So launch readiness (`AirOperationsReady`) and
withdraw readiness (`DamageWithdrawRecommended`) are separate engage gates that share this single
evaluator — see [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) for the
withdraw side.

---

## Determinism

The map is a plain `Ordinal` dictionary lookup — no RNG, no wall-clock, no enumeration-order
dependence — and defaults are fixed (`true`). `AirOperationsReady` therefore contributes to the
engage outcome (and world hash) deterministically; a scenario that marks a unit `readyForLaunch:
false` simply and reproducibly aborts its shots with `AirNotReady`.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`MvpEngagementAirNotReadyTests`](../../src/ProjectAegis.Sim.Tests/Engage/MvpEngagementAirNotReadyTests.cs) | The `AirOperationsReady == false ⇒ AirNotReady` engage gate. |
| [`WithdrawReadinessTrialResolverTests`](../../src/ProjectAegis.Sim.Tests/Scenario/WithdrawReadinessTrialResolverTests.cs) | The catalog withdraw trials `ReadinessPolicyEvaluator` merges with launch readiness. |

---

## See also

| Doc | For |
|-----|-----|
| [engagement-pipeline.md](engagement-pipeline.md) | The ordered kill chain; `AirOperationsReady` is gate 6 (`AirNotReady`). |
| [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) | The other readiness axis (HP% / withdraw) that `ReadinessPolicyEvaluator` merges in. |
| [deck-operations-runtime.md](deck-operations-runtime.md) | The air-ops FSM a launch-ready airframe drives. |
| [scenario-document-authoring.md](scenario-document-authoring.md) | Authoring scenario `metadata` (where `unitReadiness` lives). |
