---
id: S23-05
status: Complete
type: Config
priority: should-have
graphite_branch: stack/sprint23/phase-b-schema
estimate_days: 2
dependencies:
  - S23-01 I/O port stable
  - ADR-011 Phase B scope locked
owner: c-sharp-engineer / team-data
sprint: 23
req_trace: Req 21 §Mobility/Signatures/Emcon sheets; ADR-011 Phase B
last_updated: 2026-06-17
---

# Story 023-05 — Phase B Schema Foundation Spike (Export-Only)

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **Scope lock:** Export-stub spike only — import + validation + sim consumer wiring deferred to Sprint 24

## Summary

Migration stub + `CatalogSignature`/`CatalogMobility`/`CatalogEmcon` types + exporter sheet hooks for Signatures/Mobility/EMCON (read-only export; import deferred). Tracker row 21 updated.

## Acceptance Criteria

- [x] Migration applies cleanly (idempotent via `ShouldSkipMigration` guard)
- [x] `CatalogSignature` / `CatalogMobility` / `CatalogEmcon` types compile
- [x] Exporter emits empty Phase B sheets with headers (`Signatures`, `Mobility`, `Emcon`)
- [x] Unedited Phase B stub sheets do not produce spurious diff entries
- [x] Tracker row 21 updated in `Game-Requirements/implementation-tracker-2026-06-04.md`
- [x] No regression on Phase A sheet export ordering
- [x] Import behavior **not** implemented or tested (explicitly deferred)

## Verify Commands

```powershell
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform" -v minimal
```

## GitNexus Symbols to Impact-Check

| Symbol | Risk | Rule |
|--------|------|------|
| `PlatformWorkbookExporter` | HIGH | Sheet stub hooks |
| `PlatformWorkbookImporter` | HIGH | No import wiring — verify no accidental changes |
| `PlatformWorkbookValidator` | MEDIUM | Header validation metadata |
| `SqliteCatalogReader` | MEDIUM | Reader API extension if needed |
| `ICatalogReader` | HIGH | `npx gitnexus impact ICatalogReader --direction upstream` before API extension |

After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit.

## Files to Create / Modify

| Action | Path |
|--------|------|
| Create | `assets/data/catalog/migrations/008_platform_editor_phase_b.sql` |
| Create | `src/ProjectAegis.Data/Platform/CatalogSignature.cs` (or equivalent type) |
| Create | `src/ProjectAegis.Data/Platform/CatalogMobility.cs` |
| Create | `src/ProjectAegis.Data/Platform/CatalogEmcon.cs` |
| Modify | `src/ProjectAegis.Data/Platform/PlatformWorkbookExporter.cs` (Phase B sheet stubs) |
| Create | `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookPhaseBSheetTests.cs` |
| Modify | `Game-Requirements/implementation-tracker-2026-06-04.md` (row 21) |

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` (S23-05)
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- Data plan: `production/agentic/sprint-23-plan-data-2026-06-17.md` (S23-D04)
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`

## Test-Criterion Traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| Migration applies cleanly (idempotent via `ShouldSkipMigration` guard) | `PlatformWorkbookPhaseBSheetTests.Migration_008_applies_idempotently` + `SqliteCatalogReader.ShouldSkipMigration` (`008` + `platform_mobility`) | COVERED |
| `CatalogSignature` / `CatalogMobility` / `CatalogEmcon` types compile | `CatalogSignature.cs`, `CatalogMobility.cs`, `CatalogEmcon.cs` + `dotnet build` green | COVERED |
| Exporter emits empty Phase B sheets with headers | `PlatformWorkbookPhaseBSheetTests.Export_includes_Phase_B_stub_sheets_with_Req21_headers` | COVERED |
| Unedited Phase B stub sheets do not produce spurious diff entries | `PlatformWorkbookPhaseBSheetTests.Unedited_Phase_B_stub_sheets_do_not_produce_spurious_diff_entries` | COVERED |
| Tracker row 21 updated | `Game-Requirements/implementation-tracker-2026-06-04.md` row 21 — Phase B export stubs | COVERED |
| No regression on Phase A sheet export ordering | `PlatformWorkbookPhaseBSheetTests.Phase_A_sheet_order_is_unchanged_before_Phase_B_stubs` | COVERED |
| Import behavior not implemented or tested (deferred) | `PlatformWorkbookImporter.cs` — zero Phase B references; no importer Phase B tests | COVERED |

## Completion Notes

**Completed:** 2026-06-17  
**Verdict:** Complete  
**Criteria:** 7/7 passing  
**Implementation commit:** `7f8e51f` — `feat(data): Phase B schema export-only sheet stubs [S23-05]`  
**Deviations:** Catalog types placed under `src/ProjectAegis.Data/Catalog/` (not `Platform/`) — functionally equivalent; `SqliteCatalogReader` + `PlatformCatalogExportData` extended for migration guard and export payload  
**Test Evidence:** `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookPhaseBSheetTests.cs` (6 tests)  
**Verify runs (2026-06-17):**
- `dotnet test …Data.Tests… --filter "Platform"` → **51/51 PASS**
**PlatformWorkbookImporter:** No changes in S23-05 commit range — import deferred to Sprint 24 per scope lock  
**Code Review:** Skipped (lean mode)  
**Deferred to Sprint 24:** Phase B import wiring, validator headers, sim consumer, `IWriteGate` commit for Phase B staging tables