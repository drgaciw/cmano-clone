# Sprint 17 — DATA-5 GitNexus / verification

**Date:** 2026-06-04  
**Branch:** `stack/data/import-smoke`  
**Slice:** DATA-5 CMO markdown import smoke

## GitNexus impact (pre-edit)

| Symbol | Risk | Action |
|--------|------|--------|
| `CatalogWriteGate` | **HIGH** (7 upstream) | **No edits** — new code calls existing API only |
| `CmoMarkdownImporter` | **NEW** | Added under `src/ProjectAegis.Data/Import/` |

## Implementation

| Artifact | Path |
|----------|------|
| Importer | `src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs` |
| Fixture | `tools/cmano-db-crawler/fixtures/sensor-mini.md` (12 sensors) |
| Smoke tests | `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImportSmokeTests.cs` |
| Unit tests | `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterUnitTests.cs` |

Flow: parse markdown → `CatalogWriteGate.ProposeSensorBatch` → `ApproveBatch` → `SqliteCatalogReader` + `catalog_change_log` assertions.

## Verification

```text
dotnet test ProjectAegis.sln -v minimal
→ 380 passed (was 372; +8 tests)
npx gitnexus detect_changes --repo cmano-clone
→ low risk (no CatalogWriteGate signature changes)
```

## DBI acceptance (doc 06)

| Row | Gate |
|-----|------|
| Staged writes | **PASS** — import uses write gate, not direct `INSERT` from importer |
| Deterministic ordering | **PASS** — stable sort by `platform_id`, `sensor_id` |
| Import smoke ≥10 records | **PASS** — mini fixture + optional reference subset |

## Out of scope (unchanged)

- Full `sensor.md` bulk load into production DB
- Node `export-catalog-sensors.mjs` path (adjacent; still valid for JSON drops)