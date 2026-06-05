# Story 002 — Catalog JSON import drop

> **Epic:** platform-db-basepd-slice  
> **Status:** Complete

## Acceptance

- [x] `assets/data/catalog/sensors_baltic.json` schema (camelCase)
- [x] `CatalogJsonImporter.ImportToSqlite` deterministic ORDER BY insert
- [x] `CatalogSeedBootstrap` prefers JSON when present
- [x] Unit test reads JSON → SQLite → `TryGetBasePd`