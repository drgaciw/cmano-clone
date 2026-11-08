# S26-03 story-done evidence — CMO platform markdown import CLI

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-03  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CmoMarkdownImportProposer.ProposePlatformsFromMarkdown` — platform + mount batches (extend-only)
- `CatalogImportMarkdownCommand` + `Program.cs` — `--entity platform`, `--map-baltic-platform-ids`
- Baltic fixture: `tools/cmano-db-crawler/fixtures/baltic-platform-mini.md` (3 platforms, 4 mounts)
- Tests:
  - `CmoMarkdownPlatformImportTests.cs` — E2E propose/approve platform+mount
  - `CatalogImportMarkdownCommandTests.catalog_import_markdown_platform_entity_proposes_platform_and_mount_batches`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownPlatformImportTests" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "catalog_import_markdown_platform" -v minimal
# Data: 1/1 PASS; CLI: 1/1 PASS
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| Platform rows import E2E | 3 platforms committed (`u1`, `hostile-1`, `hostile-far`) | **PASS** |
| Mount batches via write gate | 4 `platform_mount` rows after approve | **PASS** |
| Stable sort / chunk 500 | 2 batches (3 platform + 4 mount records) | **PASS** |
| FK quarantine (PLE-2.3) | Existing combined-path regression in `CmoMarkdownImportGoldenTests` | **PASS** |
| Extend-only `CatalogWriteGate` | No gate source edits; `ProposePlatformBatch` reuse | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| Platform E2E import | `CmoMarkdownPlatformImportTests::ProposePlatformsFromMarkdown_stages_baltic_fixture_with_mount_batches` | COVERED |
| CLI platform entity | `CatalogImportMarkdownCommandTests::catalog_import_markdown_platform_entity_proposes_platform_and_mount_batches` | COVERED |
| Combined regression hash | `CmoMarkdownImportGoldenTests::Weapon_platform_combined_import_regression_WriteGate_and_replay_unchanged` | COVERED |