# Sprint 29 QA — Catalog Phase B → Sim Consumption (S29-06)

**Story:** `production/epics/sprint-29-catalog-sim-bridge/story-029-06-phaseb-sim-consumption.md`  
**Date:** 2026-06-18  
**Verdict:** PASS

## Scope

Wire Catalog Phase B rows (mobility, signatures, EMCON) into bounded sim validation/readiness paths via `ICatalogReader`. No direct SQLite in `ProjectAegis.Sim`.

## Sim consumption paths

| Phase B row | Sim consumer | Bounded path |
|-------------|--------------|--------------|
| **Signatures** | `PhaseBCatalogDetectionModifier` | `DetectionTrialResolver` → detection Pd trials |
| **Mobility** | `PhaseBCatalogMobilityReadinessStub` | `ReadinessPolicyEvaluator.EvaluateUnit`; `SimulationSession.PrimeEngageWorld` → `AirOperationsReady` |
| **EMCON** | `CatalogRadarEmconResolver` | `ScenarioEmconResolver`; `DeterministicDetectionLoop`; `SimulationSession.PrimeEngageWorld` → `RadarEmconActive`; `PdDetectionContactSimulator` (Baltic harness) |
| **Damage** (pre-existing) | `PhaseBCatalogDamageReadinessStub` | withdraw trials + `CatalogDamageWithdrawEngageGate` |

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| `ICatalogReader` exposes mobility/signatures/EMCON | `SqliteCatalogReader` (pre-existing); `InMemoryCatalogReader` extended with mobility/emcon + `BalticPhaseBFixture` | **PASS** |
| Sim tests PASS with catalog-sourced Phase B metadata | `PhaseBCatalogConsumerTests` (+10); `CatalogPhaseBReadinessEngageTests` (3) | **PASS** |
| Bounded validation/readiness consumes catalog rows | See table above; additive-only when rows absent | **PASS** |
| No direct SQLite in `ProjectAegis.Sim` | `rg -l "SQLite\|SqliteConnection" src/ProjectAegis.Sim/` → zero files | **PASS** |
| `ReplayGoldenSuiteTests` 6/6 unchanged | default Baltic path uses legacy fixture (no Phase B rows) | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff HEAD -- DelegationBridge.cs` empty | **PASS** |

## Test counts (verify commands)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Readiness|Magazine|Catalog|Mobility|Signature|Emcon" -v minimal   # 83/83 PASS
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|WriteGate|CatalogImport" -v minimal                                   # 145/145 PASS
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "CatalogPhaseB|CatalogMagazine" -v minimal                                        # 4/4 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal                                               # 6/6 PASS
rg -l "SQLite|SqliteConnection" src/ProjectAegis.Sim/ --glob "*.cs" || true               # (empty)
```

## Key edge cases covered

- Legacy Baltic fixture without Phase B rows → detection/envMask, mobility readiness, EMCON, engage unchanged
- Committed signature → envMask modifier affects detection Pd
- Zero-range mobility → `AirNotReady` engage abort
- Catalog EMCON posture `off` → detection roll skipped; engage `EmconOff` abort
- Scenario `unitRadarEmcon` overrides catalog EMCON for detection
- `BalticPhaseBFixture` with active posture → engage launches

## Files changed (implementation)

- `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs`
- `src/ProjectAegis.Sim/Catalog/PhaseBCatalogMobilityReadinessStub.cs` *(new)*
- `src/ProjectAegis.Sim/Catalog/CatalogRadarEmconResolver.cs` *(new)*
- `src/ProjectAegis.Sim/Scenario/ScenarioEmconResolver.cs`
- `src/ProjectAegis.Sim/Policy/ReadinessPolicyEvaluator.cs`
- `src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs`
- `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs`
- `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs`
- `src/ProjectAegis.Sim.Tests/Catalog/PhaseBCatalogConsumerTests.cs`
- `src/ProjectAegis.Delegation.Tests/Sim/CatalogPhaseBReadinessEngageTests.cs` *(new)*