# S29-10 QA Evidence — Balance Drift in Catalog Pipeline

**Story:** `production/epics/sprint-29-corpus-approve/story-029-10-balance-drift-pipeline.md`  
**Date:** 2026-06-18  
**Verdict:** **PASS**

## Scope

Surface `enableBalanceDrift` advisory on catalog import/approve diff paths. Sim default `enableBalanceDrift=false` unchanged. ReplayGolden 6/6 unchanged. No `CatalogWriteGate` bypass. ZERO touch `DelegationBridge.cs`.

## Deliverables

| Component | Path |
|-----------|------|
| Pipeline settings | `src/ProjectAegis.Data/Telemetry/CatalogBalanceDriftPipelineSettings.cs` |
| Pipeline evaluator | `src/ProjectAegis.Data/Telemetry/CatalogBalanceDriftPipelineEvaluator.cs` |
| Diff entity resolver | `src/ProjectAegis.Data/Platform/CatalogPipelineDiffEntityResolver.cs` |
| Propose/approve wiring | `src/ProjectAegis.Data/Platform/PlatformWorkbookWriteService.cs` |
| Import propose wiring | `src/ProjectAegis.Data/Import/CmoMarkdownImportProposer.cs` |
| Approve diff entity ids | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.ListStagingEntityIds` (extend-only) |
| Pipeline tests | `src/ProjectAegis.Data.Tests/Telemetry/CatalogBalanceDriftPipelineTests.cs` |

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

## Acceptance Criteria Traceability

| AC | Evidence | Status |
|----|----------|--------|
| Import/approve diff emits advisory when `enableBalanceDrift=true` | `CatalogBalanceDriftPipelineTests` propose + approve + import cases | **PASS** |
| Pipeline tests PASS on curated fixtures | Data **162/162** (filter includes 7 new pipeline tests + existing balance/WriteGate/Platform/CatalogImport) | **PASS** |
| Sim `enableBalanceDrift` default **false** unchanged | Sim Telemetry\|Balance **7/7**; no Sim layer edits | **PASS** |
| ReplayGolden 6/6 unchanged | **6/6 PASS** | **PASS** |
| No `CatalogWriteGate` bypass | `Approve_diff_advisory_does_not_block_write_gate_commit`; advisory-only evaluator | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Edge Cases Covered

| Case | Test |
|------|------|
| Drift beyond ±8% | `Propose_diff_surfaces_drift_advisory_when_enabled_beyond_eight_percent` |
| Exactly at threshold | `Drift_at_exactly_eight_percent_band_emits_no_pipeline_advisory` |
| Empty diff | `Propose_empty_diff_emits_no_drift_findings_when_enabled` |
| Default flag off | `Propose_diff_default_disabled_emits_empty_advisory` |
| Approve path | `Approve_diff_surfaces_drift_advisory_when_enabled` |
| Import propose path | `Import_propose_surfaces_drift_advisory_for_touched_platform` |

## Per-Project Counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Data.Tests | WriteGate\|Platform\|CatalogImport\|Balance | 162 |
| ProjectAegis.Sim.Tests | Telemetry\|Balance | 7 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Verdict

**COMPLETE** — Balance drift advisory surfaced on catalog import/approve diff paths; default off; replay 6/6 unchanged; no write-gate bypass.