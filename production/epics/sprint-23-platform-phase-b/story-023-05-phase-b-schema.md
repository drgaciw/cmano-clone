---
id: S23-05
status: Ready
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
---

# Story 023-05 — Phase B Schema Foundation Spike (Export-Only)

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **Scope lock:** Export-stub spike only — import + validation + sim consumer wiring deferred to Sprint 24

## Summary

Migration stub + `CatalogSignature`/`CatalogMobility`/`CatalogEmcon` types + exporter sheet hooks for Signatures/Mobility/EMCON (read-only export; import deferred). Tracker row 21 updated.

## Acceptance Criteria

- [ ] Migration applies cleanly (idempotent via `ShouldSkipMigration` guard)
- [ ] `CatalogSignature` / `CatalogMobility` / `CatalogEmcon` types compile
- [ ] Exporter emits empty Phase B sheets with headers (`Signatures`, `Mobility`, `Emcon`)
- [ ] Unedited Phase B stub sheets do not produce spurious diff entries
- [ ] Tracker row 21 updated in `Game-Requirements/implementation-tracker-2026-06-04.md`
- [ ] No regression on Phase A sheet export ordering
- [ ] Import behavior **not** implemented or tested (explicitly deferred)

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