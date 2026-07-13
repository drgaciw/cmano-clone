# Sprint 34 — Data Track Plan

**Owner:** team-data  
**Must-have:** S34-02, S34-03 (~3.5d)  
**Should-have:** S34-05, S34-08  
**Sprint gate:** LinkCatalog workbook empty-diff golden

## Stories

| ID | Name | Est. | Priority | Deps |
|----|------|------|----------|------|
| S34-02 | Link catalog data model + write-gate | 2d | must | S34-01 |
| S34-03 | LinkCatalog workbook export/import/diff | 1.5d | must | S34-02 |
| S34-05 | Link FK + validation rules (`LINK_*`) | 1d | should | S34-03 |
| S34-08 | `catalog_link_report` CLI (read-only) | 0.5d | should | S34-02 |

## Gap (S33 → S34)

`link_catalog` table exists (migration `007`) but lacks C# reader, staging, and workbook I/O. S33-06 closed `platform_comms` only.

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|LinkCatalog|Platform|Comms" -v minimal
npx gitnexus impact CatalogWriteGate PlatformWorkbookExporter
```

## Cut line

1. S34-08 CLI  
2. S34-05 validation (keep if capacity — first should-have restore)