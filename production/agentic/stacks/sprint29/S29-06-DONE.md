# S29-06 story-done — Catalog Phase B → Sim Consumption

**Story:** `production/epics/sprint-29-catalog-sim-bridge/story-029-06-phaseb-sim-consumption.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `ICatalogReader` exposes mobility/signatures/EMCON | `SqliteCatalogReader` + extended `InMemoryCatalogReader` / `BalticPhaseBFixture` | COVERED |
| Sim tests PASS with catalog-sourced Phase B metadata | `PhaseBCatalogConsumerTests`; `CatalogPhaseBReadinessEngageTests` | COVERED |
| Bounded validation/readiness consumes catalog rows | mobility → readiness/engage; EMCON → detection/engage; signatures → detection (S24-06) | COVERED |
| No direct SQLite in `ProjectAegis.Sim` | `rg` zero hits | COVERED |
| `ReplayGoldenSuiteTests` 6/6 unchanged | default Baltic legacy fixture | COVERED |
| Evidence QA doc | `production/qa/sprint-29-catalog-sim-bridge-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge` | empty diff | COVERED |

## Sim paths now consuming Phase B

1. **Signatures** — `DetectionTrialResolver` + `PhaseBCatalogDetectionModifier` (S24-06, unchanged)
2. **Mobility** — `ReadinessPolicyEvaluator.EvaluateUnit`; `SimulationSession.PrimeEngageWorld` (`AirOperationsReady`)
3. **EMCON** — `CatalogRadarEmconResolver` via `ScenarioEmconResolver`; `DeterministicDetectionLoop`; `SimulationSession.PrimeEngageWorld` (`RadarEmconActive`); Baltic `PdDetectionContactSimulator` catalog pass-through

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Readiness|Magazine|Catalog|Mobility|Signature|Emcon" -v minimal   # 83/83
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|WriteGate|CatalogImport" -v minimal                                   # 145/145
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal                                               # 6/6
rg -l "SQLite|SqliteConnection" src/ProjectAegis.Sim/ --glob "*.cs" || true
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

Evidence: `production/qa/sprint-29-catalog-sim-bridge-2026-06-18.md`