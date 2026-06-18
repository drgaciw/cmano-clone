---
id: S23-04
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint23/approve-batch-multi
estimate_days: 2.5
dependencies:
  - S23-02 green baseline
  - S22-01 / S22-04 staging tables exist (007_platform_editor_phase_a.sql)
owner: c-sharp-engineer / team-data
sprint: 23
req_trace: DBI-1.1, DBI-1.4, DBI-4.1, PLE-3.1–3.5
last_updated: 2026-06-17
---

# Story 023-04 — ApproveBatch Multi-Entity Commit Path

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **ADR:** ADR-006 (write-gate); extend-only mandate on `CatalogWriteGate`

## Summary

Extend `CatalogWriteGate.LoadStagingRows` + `ApproveBatch` beyond sensor-only to commit staged platform/weapon/mount/loadout/magazine/comms rows; DBI-1.4 orphan guard on reject. Closes Sprint 22 sign-off **C3**. Defer cleanly to Sprint 24 if must-haves consume buffer.

## Acceptance Criteria

- [x] `ProposePlatformBatch` → `ApproveBatch` commits to live tables
- [x] Mount/loadout/magazine/comms commit paths implemented
- [x] `RejectBatch` purges all staging tables (DBI-1.4) — no orphan rows
- [x] CmoMarkdown platform import E2E test PASS
- [x] `WriteGate` filter green
- [x] Extend-only on `CatalogWriteGate` — sensor path regression unchanged
- [x] GitNexus impact on `CatalogWriteGate` (CRITICAL) documented before merge
- [x] No importer or MCP path auto-commits without `ApproveBatch` (PLE-3.1)

## Verify Commands

```powershell
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "WriteGate|Platform|CmoMarkdown" -v minimal
npx gitnexus impact CatalogWriteGate --direction upstream
```

## GitNexus Symbols to Impact-Check

| Symbol | Risk | Rule |
|--------|------|------|
| `CatalogWriteGate` | **CRITICAL** | Extend-only `LoadStagingRows`/`ApproveBatch`; impact upstream **before** edit |
| `IWriteGate` | HIGH | Public write-gate contract |
| `ApproveBatch` | CRITICAL | Multi-entity commit extension |
| `LoadStagingRows` | CRITICAL | Entity-aware staging probe |
| `DeleteStagingRows` | HIGH | Reject purge all staging tables |
| `CmoMarkdownImportProposer` | MEDIUM | E2E propose path |
| `PlatformWorkbookImporter` | MEDIUM | Workbook staging path |
| `SqliteCatalogReader` / `ICatalogReader` | MEDIUM | Post-commit readback |

After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit.

## Files to Create / Modify

| Action | Path |
|--------|------|
| Modify | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` (`LoadStagingRows`, `ApproveBatch`, upsert helpers) |
| Extend | `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` (sensor regression) |
| Create | `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGatePlatformApproveTests.cs` |
| Extend | `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs` (approve → readback) |
| Extend | `src/ProjectAegis.Data.Tests/Snapshots/DbSnapshotBindingTests.cs` (post-commit snapshot) |

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` (S23-04)
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- Data plan: `production/agentic/sprint-23-plan-data-2026-06-17.md` (S23-D01)
- Req 06: `Game-Requirements/requirements/06-Database-Intelligence.md`

## Test-Criterion Traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| `ProposePlatformBatch` → `ApproveBatch` commits to live tables | `CatalogWriteGatePlatformApproveTests.ApproveBatch_platform_batch_commits_to_live_table` | COVERED |
| Mount/loadout/magazine/comms commit paths | `CatalogWriteGatePlatformApproveTests.ApproveBatch_mount_loadout_magazine_comms_commit_paths` + `ApproveBatch_weapon_batch_commits_to_live_table` | COVERED |
| `RejectBatch` purges all staging tables (DBI-1.4) | `CatalogWriteGatePlatformApproveTests.RejectBatch_purges_all_staging_tables_DBI_1_4` + `CmoMarkdownImporterTests.Reject_platform_batch_removes_all_staging_rows_DBI_1_4_orphan_guard` | COVERED |
| CmoMarkdown platform import E2E test PASS | `CmoMarkdownImporterTests.ProposePlatformWeaponMounts_approve_readback_reflects_live_rows_in_stable_order` | COVERED |
| `WriteGate` filter green | `dotnet test … --filter "WriteGate"` → **9/9 PASS** | COVERED |
| Extend-only — sensor path regression unchanged | `CatalogWriteGateTests` (3 sensor tests) + `ApproveSensorStaging` preserved in `CatalogWriteGate.cs` | COVERED |
| GitNexus impact on `CatalogWriteGate` (CRITICAL) documented | Commit `aa36dc9` body: CRITICAL, 51 symbols, 14 processes, 9 modules | COVERED |
| No importer/MCP auto-commit without `ApproveBatch` (PLE-3.1) | `CmoMarkdownImportProposer.ProposePlatformWeaponMounts` docstring + `PlatformWorkbookImporter` PLE-3.1 comment; propose-only tests | COVERED |

## Completion Notes

**Completed:** 2026-06-17  
**Verdict:** Complete  
**Criteria:** 8/8 passing  
**Implementation commit:** `aa36dc9` — `feat(data): ApproveBatch multi-entity commit path [S23-04]`  
**Deviations:** None — extend-only `CatalogWriteGate`; sensor `ApproveSensorStaging` path preserved verbatim  
**Test Evidence:**
- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGatePlatformApproveTests.cs` (5 tests)
- `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` (3 sensor regression tests)
- `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs` (E2E approve → readback extension)
**Verify runs (2026-06-17):**
- `dotnet test …Data.Tests… --filter "WriteGate|Platform|CmoMarkdown"` → **63/63 PASS**
- `dotnet test …Data.Tests… --filter "WriteGate"` → **9/9 PASS**
**GitNexus:** `npx gitnexus impact CatalogWriteGate --direction upstream --repo cmano-clone` → **CRITICAL**, 51 symbols, 14 processes, 9 modules (documented in `aa36dc9`)  
**Code Review:** Skipped (lean mode)  
**Closes:** Sprint 22 sign-off **C3** (ApproveBatch platform/weapon commit path)