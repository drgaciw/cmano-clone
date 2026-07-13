# S31-09 story-done — Balance Drift Advisory on Nightly Approve

**Story:** `production/epics/sprint-31-corpus-approve-complete/story-031-09-balance-drift-nightly.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/balance-drift-nightly`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Nightly approve summary includes balance drift advisory when `enableBalanceDrift=true` | `NightlyApproveBalanceDriftSummaryTests` + `cmo-nightly-approve.sh --enable-balance-drift` | COVERED |
| `enableBalanceDrift` default **false** — no advisory on default path | `Nightly_approve_summary_default_disabled_omits_advisory`; CLI default omits field | COVERED |
| Pipeline tests PASS on curated fixtures | Data **173/173** (filter WriteGate\|Platform\|CatalogImport\|Balance) | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 unchanged | **6/6 PASS** | COVERED |
| No `CatalogWriteGate` bypass | Advisory evaluated before approve; commit unaffected | COVERED |
| Evidence doc | `production/qa/sprint-31-balance-drift-nightly-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## Deliverables

| Component | Path |
|-----------|------|
| Nightly approve drift evaluator | `src/ProjectAegis.Data/Import/NightlyApproveBalanceDriftSummary.cs` |
| CLI approve flag | `src/ProjectAegis.MissionEditor.Cli/CatalogWriteApproveCommand.cs` |
| Shell flag + summary aggregation | `tools/cmo-nightly-approve.sh` (`--enable-balance-drift`, default off) |
| Data tests | `src/ProjectAegis.Data.Tests/Import/NightlyApproveBalanceDriftSummaryTests.cs` |
| CLI tests | `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Data.Tests \
  --filter "WriteGate|Platform|CatalogImport|Balance" -v minimal
# Passed: 173/173

dotnet test src/ProjectAegis.Sim.Tests \
  --filter "Telemetry|Balance" -v minimal
# Passed: 7/7

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

./tools/cmo-nightly-approve.sh --entity sensor --dry-run --enable-balance-drift

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Data.Tests | WriteGate\|Platform\|CatalogImport\|Balance | 173 |
| ProjectAegis.Sim.Tests | Telemetry\|Balance | 7 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Not touched (by design)

- `CatalogWriteGate.cs` (no bypass; existing `ListStagingEntityIds` reused)
- `DelegationBridge.cs`