# S29-10 story-done evidence — Balance Drift in Catalog Pipeline

**Story:** `production/epics/sprint-29-corpus-approve/story-029-10-balance-drift-pipeline.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- `CatalogBalanceDriftPipelineSettings` — pipeline feature surface; `enableBalanceDrift` default **false**
- `CatalogBalanceDriftPipelineEvaluator` — advisory-only drift evaluation filtered to diff entity ids
- `CatalogPipelineDiffEntityResolver` — extracts touched platform ids from import plan / import batches
- `PlatformWorkbookWriteService` — attaches advisory on propose + approve diff paths
- `CmoMarkdownImportProposer` — attaches advisory on import propose path
- `CatalogWriteGate.ListStagingEntityIds` — extend-only helper for approve diff entity resolution
- Tests: `CatalogBalanceDriftPipelineTests` (7 cases)
- **ZERO** touch `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Balance" -v minimal
# Passed: 162/162

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
# Passed: 7/7

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Import/approve diff emits advisory when flag enabled | `CatalogBalanceDriftPipelineTests` | **PASS** |
| Pipeline tests PASS | Data **162/162** | **PASS** |
| `enableBalanceDrift` Sim default **false** | No Sim edits; Telemetry\|Balance **7/7** | **PASS** |
| ReplayGoldenSuiteTests 6/6 | 6/6 PASS | **PASS** |
| No `CatalogWriteGate` bypass | Advisory-only; approve still commits | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Data.Tests | WriteGate\|Platform\|CatalogImport\|Balance | 162 |
| ProjectAegis.Sim.Tests | Telemetry\|Balance | 7 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Verdict

**COMPLETE** — S28-10 balance drift advisory extended to catalog import/approve pipeline; default off; replay 6/6 unchanged; no write-gate bypass.

**QA evidence:** `production/qa/sprint-29-balance-drift-pipeline-2026-06-18.md`