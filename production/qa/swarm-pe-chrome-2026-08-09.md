# SWARM-21 PE chrome — Swarms sheet write-gate (2026-08-09)

**Issue:** DRG-110 · **Epic:** DRG-83 · **Req:** SWARM-21 Phase B residual

## Delivered

- Platform workbook **Swarms** sheet export (headers + CEC/mode/host fields)
- `LoadExportData` / TL filter include `CatalogSwarmPlatform` rows
- Validator: header parity, orphan platform, invalid mode, MaxDrones > 0
- Importer stages via `ProposeSwarmBatch` → `catalog_staging_swarm` (migration 014)
- Approve upserts `platform_swarm` (incl. `cec_capable`)

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter PlatformWorkbookSwarmPeTests
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "FullyQualifiedName~PlatformWorkbook"
```

## Notes

- Generic swarm remains `cecCapable=0`; USN exemplar remains CEC-capable.
- Unity PE shell stays thin binder; sheet chrome is the authoring SoT (ADR-011).
