# S24-06 story-done evidence — phase-b-sim-consumer

**Branch:** `stack/sprint24/phase-b-sim-consumer`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-06  
**Status:** Complete

## Deliverables

- `PhaseBCatalogDetectionModifier`: consumes `ICatalogReader.TryGetSignature(observerId)`; applies `RcsBandDbsm` as deterministic env-mask multiplier (neutral at -10 dBsm)
- `DetectionTrialResolver.Resolve`: applies modifier after catalog `basePd` resolution (additive when TryGet misses)
- `InMemoryCatalogReader`: optional Phase B signature rows for test fixtures (Baltic fixture unchanged)
- `PhaseBCatalogConsumerTests.cs`: legacy unchanged, signature delta, boundary RCS, unreferenced platform
- **ZERO** touch `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "PhaseB|DetectionTrial" -v minimal
# Passed: 8

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6 — replay hashes unchanged (Baltic catalog has no Phase B signature rows)
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| `ICatalogReader.TryGet*` consumed by sim detection modifier stub | **PASS** — `PhaseBCatalogDetectionModifier.Apply` |
| Committed signature affects detection trial outcome | **PASS** — envMask + `DeterministicDetectionLoop` pd delta test |
| Fixtures without Phase B data unchanged (additive-only) | **PASS** — Baltic fixture + ReplayGolden 6/6 |
| `/replay-verify` mandatory — ReplayGoldenSuiteTests 6/6 PASS | **PASS** |
| ZERO touch `DelegationBridge.cs` | **PASS** |

## Completion Notes
**Completed**: 2026-06-17
**Criteria**: 5/5 passing (0 deferred)
**Deviations**: None
**Test Evidence**: Integration/Logic — `src/ProjectAegis.Sim.Tests/Catalog/PhaseBCatalogConsumerTests.cs` (6 tests) + `src/ProjectAegis.Sim.Tests/Scenario/DetectionTrialResolverTests.cs` (2 tests); replay gate `ReplayGoldenSuiteTests` 6/6 PASS
**Code Review**: Skipped (lean mode) — run `/code-review` on `PhaseBCatalogDetectionModifier.cs`, `DetectionTrialResolver.cs` before stack merge