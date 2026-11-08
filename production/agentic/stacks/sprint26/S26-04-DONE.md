# S26-04 story-done evidence — import E2E + golden hygiene

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-04  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CmoMarkdownImportGoldenTests.cs` — snapshot hash stability, WriteGate regression, sensor path unchanged
- Golden pin: `CatalogSortKeyGoldenHashes.BalticCmoImport` ordering hash preserved after weapon/platform CLI additions
- ReplayGolden 6/6 unchanged (no sim-layer edits)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownImportGoldenTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Golden: 3/3 PASS; ReplayGolden: 6/6 PASS
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| Re-import empty diff / hash stable | `Weapon_import_reapprove_identical_slice_preserves_catalog_ordering_hash` | **PASS** |
| WriteGate regression PASS | `Weapon_platform_combined_import_regression_WriteGate_and_replay_unchanged` | **PASS** |
| Sensor+Phase A/B+damage regression unchanged | `Sensor_import_path_unchanged_after_weapon_platform_cli_additions` | **PASS** |
| Replay 6/6 unchanged | `ReplayGoldenSuiteTests` — no sim changes | **PASS** |
| Validation/snapshot hash golden | `CatalogSortKeyGoldenHashes.BalticCmoImport` asserted | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| Re-import hash stability | `CmoMarkdownImportGoldenTests::Weapon_import_reapprove_identical_slice_preserves_catalog_ordering_hash` | COVERED |
| Combined WriteGate regression | `CmoMarkdownImportGoldenTests::Weapon_platform_combined_import_regression_WriteGate_and_replay_unchanged` | COVERED |
| Sensor path unchanged | `CmoMarkdownImportGoldenTests::Sensor_import_path_unchanged_after_weapon_platform_cli_additions` | COVERED |
| Replay golden | `ReplayGoldenSuiteTests` (6 tests) | COVERED |