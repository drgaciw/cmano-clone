---
id: S23-04
status: Ready
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
---

# Story 023-04 — ApproveBatch Multi-Entity Commit Path

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **ADR:** ADR-006 (write-gate); extend-only mandate on `CatalogWriteGate`

## Summary

Extend `CatalogWriteGate.LoadStagingRows` + `ApproveBatch` beyond sensor-only to commit staged platform/weapon/mount/loadout/magazine/comms rows; DBI-1.4 orphan guard on reject. Closes Sprint 22 sign-off **C3**. Defer cleanly to Sprint 24 if must-haves consume buffer.

## Acceptance Criteria

- [ ] `ProposePlatformBatch` → `ApproveBatch` commits to live tables
- [ ] Mount/loadout/magazine/comms commit paths implemented
- [ ] `RejectBatch` purges all staging tables (DBI-1.4) — no orphan rows
- [ ] CmoMarkdown platform import E2E test PASS
- [ ] `WriteGate` filter green
- [ ] Extend-only on `CatalogWriteGate` — sensor path regression unchanged
- [ ] GitNexus impact on `CatalogWriteGate` (CRITICAL) documented before merge
- [ ] No importer or MCP path auto-commits without `ApproveBatch` (PLE-3.1)

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