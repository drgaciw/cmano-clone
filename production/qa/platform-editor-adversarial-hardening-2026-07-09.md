# Platform Editor adversarial TDD hardening — Track CROSS — 2026-07-09

**Branch:** `feat/pe-hard-cross`  
**Mode:** Cross-cutting integration / product-invariant pins (PE-W1 + PE-W2)  
**Scope:** Tests-only under `src/ProjectAegis.Data.Tests/Platform/`  
**Production edits:** none  
**Forbidden surfaces:** CatalogWriteGate write paths, DelegationBridge, replay goldens, hash files — untouched

## Attack vectors pinned

| # | Vector | Test method | Result |
|---|--------|-------------|--------|
| 1 | Empty-diff golden after ClosedXML Write→Read (unedited Baltic-like) | `Empty_diff_golden_holds_after_ClosedXml_write_read_unedited_baltic_export` | GREEN (already hard) |
| 2 | Export→Write→Read→Plan never blocked solely by PE-W1 protection/validation metadata | `Export_Write_Read_Plan_never_blocked_solely_by_protection_or_validation_metadata` | GREEN (already hard) |
| 3 | WorkbookHash / SourceSnapshotId survive ClosedXML + PE-W1 protection | `WorkbookHash_and_SourceSnapshotId_survive_ClosedXml_round_trip_with_protection` | GREEN (already hard) |
| 4 | PE-W2 quarantine empty on unedited empty-diff plan | `Unedited_workbook_quarantine_empty_and_empty_diff_orthogonal` | GREEN (already hard) |
| 5 | RequiresHumanApproval independent of quarantine on separate orphan edit | `RequiresHumanApproval_independent_of_quarantine_on_separate_orphan_edit` | GREEN (already hard) |
| 6 | Sim-visible CatalogTlExportFilter + Excel path does not reintroduce provisional | `Sim_visible_filter_excel_export_path_does_not_reintroduce_provisional` | GREEN (already hard) |
| 7 | Determinism: two Plan() calls equal change + quarantine counts | `Plan_twice_on_same_workbook_yields_equal_change_and_quarantine_counts` | GREEN (already hard) |
| 8 | FakeWriteGate never proposed on blocked plan (over-capacity + FK quarantine) | `FakeWriteGate_never_proposed_on_blocked_plan_findings_quarantine_consistent` + `FakeWriteGate_never_proposed_when_fk_quarantine_blocks_plan` | GREEN (already hard) |
| + | Double ClosedXML round-trip remains empty-diff / empty plan | `ClosedXml_double_round_trip_remains_empty_diff_and_empty_plan` | GREEN (already hard) |

## File added

- `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookPeIntegrationHardeningTests.cs` (10 facts)

## Verify (RUN+READ)

```text
dotnet test src/ProjectAegis.Data.Tests/ --filter "FullyQualifiedName~Platform|FullyQualifiedName~PeIntegration|FullyQualifiedName~Hardening"
  → Passed: 168 / 0 failures

dotnet test src/ProjectAegis.Data.Excel.Tests/
  → Passed: 13 / 0 failures

dotnet build ProjectAegis.sln
  → 0 Warning(s), 0 Error(s)
```

Targeted filter:

```text
FullyQualifiedName~PlatformWorkbookPeIntegrationHardeningTests
  → Passed: 10 / 0 failures
```

## Invariant proof summary

| Invariant | Proof |
|-----------|--------|
| PLE-2.1 empty-diff | ClosedXML Write→Read of unedited Baltic-like export: `PlatformWorkbookDiff.IsEmpty` + `Plan.HasChanges == false` |
| PE-W1 metadata non-blocking | `_Meta` protected after Write; Plan after Read has zero Error findings |
| Hash / snapshot stability | `SourceSnapshotId`, `SchemaVersion`, `WorkbookHash` (meta + `PlatformWorkbookHash.Compute`) survive round-trip under protection |
| Quarantine ⊥ empty-diff | Unedited plan: empty changes **and** empty `QuarantineEntries`; Stage proposes 0 |
| Human approval ⊥ quarantine | `changes.Count > HumanApprovalRecordThreshold` with separate orphan mount FK: `RequiresHumanApproval && Blocked && QuarantineEntries nonempty` |
| Sim-visible provisional exclusion | `CatalogTlExportFilter.Apply` drops provisional; Excel round-trip of filtered export has no provisional SensorId/ReviewState |
| Determinism | Two `Plan()` on same workbook: equal change/quarantine/finding counts and ordered tuples |
| Stage refuse on blocked | Over-capacity + FK quarantine: `Staged=false`, all FakeWriteGate proposal lists empty, all batch ids null |

## Notes

- All pins went GREEN on first run — PE-W1/W2 surfaces are **already hard** for these cross invariants; tests retained as adversarial regression nets.
- No production code fixes required.

## Orchestrator integration (post-merge)

| Check | Result |
|-------|--------|
| Full solution | **1599** passed / 0 failed |
| Excel.Tests | 24 |
| Data.Tests | 616 |
| PlayModeSmoke+ReplayGolden | 37 |
| Hash `17144800277401907079` | 18 files |
| DelegationBridge / CatalogWriteGate rewrite | ZERO |

### Real bugs fixed by adversarial tracks

1. **ToExcelList** — reject comma/quote/newline tokens that corrupt Excel list formulas
2. **ErrorStyle** — Warning (soft UX) instead of default Stop
3. **Double ApproveBatches** — no second `db_release` bind; `batch_already_committed_or_not_pending`
4. **Quarantine sort** — EntityKind → PlatformId → EntityId → Reason
5. **Doc 21 honesty contract** — PE completion language supersedes Wave 3 footer pin

### Residual honesty

- OQ5 sheet protect remains passwordless soft UX
- CatalogWriteGate may still re-upsert staging if called directly; WriteService blocks release double-bind
- Phase N Live Editor screenshots still residual

