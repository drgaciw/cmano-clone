# Story 004 — Catalog provenance columns

> **Epic:** platform-db-basepd-slice  
> **Status:** Complete  
> **Last Updated:** 2026-06-08  
> **ADR:** ADR-006

## Acceptance

- [x] `import_batch_id`, `source_file` on `sensor` table (migration 001)
- [x] JSON `importBatchId` + per-drop `source_file` in SQLite
- [x] `CatalogProvenanceMigrationTests`

## Test evidence

- `assets/data/catalog/migrations/001_sensor_base_pd.sql`
- `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs`
- `src/ProjectAegis.Data.Tests/Catalog/CatalogProvenanceMigrationTests.cs`

## Completion Notes

**Completed:** 2026-06-08  
**Criteria:** 3/3 passing  
**Test Evidence:** Paths above; `Sqlite_reader_loads_provenance_columns` green (1/1)  
**Code Review:** Full mode — LP-CODE-REVIEW APPROVED (ADR-006 req-06: provenance columns on `sensor`, import path stamps `importBatchId`/`source_file`, auditable read via `CatalogSensorBinding`)  
**QL-TEST-COVERAGE:** ADEQUATE — AC1→migration 001 schema; AC2→`CatalogJsonImporter.ReadSensorBindings` batch/file stamping; AC3→`CatalogProvenanceMigrationTests`