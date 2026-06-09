# Story 002 — Catalog JSON import drop

> **Epic:** platform-db-basepd-slice  
> **Status:** Complete  
> **Last Updated:** 2026-06-08  
> **ADR:** ADR-006

## Acceptance

- [x] `assets/data/catalog/sensors_baltic.json` schema (camelCase)
- [x] `CatalogJsonImporter.ImportToSqlite` deterministic ORDER BY insert
- [x] `CatalogSeedBootstrap` prefers JSON when present
- [x] Unit test reads JSON → SQLite → `TryGetBasePd`

## Test evidence

- `assets/data/catalog/sensors_baltic.json`
- `src/ProjectAegis.Data/Catalog/CatalogJsonImporter.cs`
- `src/ProjectAegis.Data/Catalog/CatalogSeedBootstrap.cs`
- `src/ProjectAegis.Data.Tests/Catalog/CatalogJsonImporterTests.cs`
- `src/ProjectAegis.Data.Tests/Catalog/CatalogSeedBootstrapTests.cs`

## Completion Notes

**Completed:** 2026-06-08  
**Criteria:** 4/4 passing  
**Test Evidence:** Paths above; `CatalogJsonImporterTests` + `CatalogSeedBootstrapTests` green (2/2)  
**Code Review:** Full mode — LP-CODE-REVIEW APPROVED (ADR-006: import via Data-layer `CatalogJsonImporter`, camelCase DTO, deterministic sort before insert, read-only `SqliteCatalogReader` consumer)  
**QL-TEST-COVERAGE:** ADEQUATE — AC1→`sensors_baltic.json`; AC2→`ImportToSqlite_reads_json_drop_deterministically`; AC3→`SeedBalticPatrol_writes_sorted_sqlite_catalog` (JSON path when file present); AC4→`TryGetBasePd` assertion in importer test