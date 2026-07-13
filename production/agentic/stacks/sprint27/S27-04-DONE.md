# S27-04 story-done — Import E2E + golden hygiene

**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CatalogSortKeyGoldenHashes.BalticCmoImportWithFittings` pinned
- `CmoMarkdownImportGoldenTests` — fittings regression + re-import stability
- ReplayGolden unchanged (6/6)

## Verify

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownImportGoldenTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## Verdict: COMPLETE

| AC | Test | Status |
|----|------|--------|
| Golden hash with fittings | `Weapon_platform_combined_import_regression_*` | COVERED |
| Re-import stable hash | `Platform_fittings_reimport_identical_slice_*` | COVERED |
| Sensor path unchanged | `Sensor_import_path_unchanged_*` | COVERED |
| ReplayGolden 6/6 | `ReplayGoldenSuiteTests` | COVERED |