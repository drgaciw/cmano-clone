# Story 003 — Catalog bulk JSON import directory

> **Epic:** platform-db-basepd-slice  
> **Status:** Complete  
> **Last Updated:** 2026-06-08  
> **ADR:** ADR-006

## Acceptance

- [x] `assets/data/catalog/import/*.json` drop folder
- [x] `CatalogBulkImporter.ImportDirectory` merges deterministically
- [x] Unit test imports directory → SQLite

## Test evidence

- `assets/data/catalog/import/sensors_baltic.json`
- `assets/data/catalog/import/sensor_quarantine_sample.json`
- `src/ProjectAegis.Data/Catalog/CatalogBulkImporter.cs`
- `src/ProjectAegis.Data.Tests/Catalog/CatalogBulkImporterTests.cs`

## Completion Notes

**Completed:** 2026-06-08  
**Criteria:** 3/3 passing  
**Test Evidence:** Paths above; `ImportDirectory_merges_sorted_json_drops` green (1/1)  
**Code Review:** Full mode — LP-CODE-REVIEW APPROVED (ADR-006: bulk import stays in Data, files merged with ordinal sort, quarantine partition via `CatalogImportGate`)  
**QL-TEST-COVERAGE:** ADEQUATE — AC1→`import/*.json` assets; AC2→`CatalogBulkImporter.ImportDirectory` ordinal file order + last-wins merge; AC3→`CatalogBulkImporterTests`