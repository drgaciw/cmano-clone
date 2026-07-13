# S31-09 QA Evidence — Balance Drift Advisory on Nightly Approve

**Story:** `production/epics/sprint-31-corpus-approve-complete/story-031-09-balance-drift-nightly.md`  
**Date:** 2026-06-18  
**Verdict:** **PASS**

## Scope

Wire S29-10 `CatalogBalanceDriftPipelineEvaluator` into nightly approve summary JSON when `enableBalanceDrift=true`. Default off on nightly path. No `CatalogWriteGate` bypass. ReplayGolden 6/6 unchanged. ZERO touch `DelegationBridge.cs`.

## Deliverables

| Component | Path |
|-----------|------|
| Nightly approve drift evaluator + DTO | `src/ProjectAegis.Data/Import/NightlyApproveBalanceDriftSummary.cs` |
| CLI `--enable-balance-drift` on `catalog_write_approve` | `CatalogWriteApproveCommand.cs`, `Program.cs` |
| Shell `--enable-balance-drift` (default false) | `tools/cmo-nightly-approve.sh` |
| Entity + final summary aggregation | `write_entity_summary` / `write_final_summary` Python blocks |
| Data tests (on/off paths) | `NightlyApproveBalanceDriftSummaryTests.cs` (5 cases) |
| CLI tests | `CatalogWriteCommandTests.cs` (+2 cases) |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|CatalogImport|Balance" -v minimal
# Passed: 173/173

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Telemetry|Balance" -v minimal
# Passed: 7/7

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

./tools/cmo-nightly-approve.sh --entity sensor --dry-run --enable-balance-drift
# Balance drift advisory: enabled; per-batch dry-run includes --enable-balance-drift

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Acceptance Criteria Traceability

| AC | Evidence | Status |
|----|----------|--------|
| Advisory on nightly approve summary when flag enabled | `Nightly_approve_summary_surfaces_drift_advisory_when_enabled_beyond_eight_percent`; shell dry-run | **PASS** |
| `enableBalanceDrift` default **false** | `Nightly_approve_summary_default_disabled_omits_advisory`; `catalog_write_approve_default_omits_balance_drift_advisory` | **PASS** |
| Pipeline tests PASS | Data **173/173** | **PASS** |
| ReplayGolden 6/6 unchanged | **6/6 PASS** | **PASS** |
| No `CatalogWriteGate` bypass | `Nightly_sensor_slice_advisory_does_not_block_write_gate_commit` | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Edge Cases Covered

| Case | Test / Evidence |
|------|-----------------|
| Drift beyond ±8% | `Nightly_approve_summary_surfaces_drift_advisory_when_enabled_beyond_eight_percent` |
| Default flag off | `Nightly_approve_summary_default_disabled_omits_advisory` |
| Empty diff / no findings | `Nightly_approve_enabled_empty_diff_emits_no_findings` |
| Multi-batch aggregation | `Nightly_approve_multi_batch_summary_aggregates_entity_advisory_when_enabled` |
| Write-gate commit unaffected | `Nightly_sensor_slice_advisory_does_not_block_write_gate_commit` |
| CLI JSON shape | `catalog_write_approve_enable_balance_drift_includes_advisory_payload` |

## Per-Project Counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Data.Tests | WriteGate\|Platform\|CatalogImport\|Balance | 173 |
| ProjectAegis.Sim.Tests | Telemetry\|Balance | 7 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Verdict

**COMPLETE** — Balance drift advisory wired into nightly approve summary; default off; replay 6/6 unchanged; no write-gate bypass.