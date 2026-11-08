# S27-14 story-done evidence — Curated platform corpus slice

**Story:** `production/epics/sprint-27-cmo-corpus-import/story-027-14-platform-corpus-slice.md`  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- Fixture: `tools/cmano-db-crawler/fixtures/ship-slice-100.md` (100 synthetic ship platforms, `/ship/4001/`–`/ship/4100/`)
- `CmoMarkdownImporter.ResolveShipSlice100FixturePath()`
- Tests extended in `CmoMarkdownPlatformImportTests.cs`:
  - E2E propose/approve for ship-slice-100
  - `ChunkPlatforms_with_501_rows_produces_two_batches_at_chunk_size_500`
  - `ProposePlatformsFromMarkdown_emits_fitting_quarantine_json_for_orphan_weapon`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownPlatform|CmoMarkdownShip" -v minimal
# 4/4 PASS
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| ≥100 platforms in curated fixture | `ship-slice-100.md` — 100 `### Test Ship` sections | **PASS** |
| Chunk boundary 501→2 batches | `ChunkPlatforms_with_501_rows_produces_two_batches_at_chunk_size_500` | **PASS** |
| Quarantine report JSON on invalid rows | `ProposePlatformsFromMarkdown_emits_fitting_quarantine_json_for_orphan_weapon` writes `fittingQuarantineReport` JSON | **PASS** |
| Scoped CmoMarkdown tests PASS | `--filter "CmoMarkdownPlatform\|CmoMarkdownShip"` | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| Ship-slice E2E import | `CmoMarkdownPlatformImportTests::ProposePlatformsFromMarkdown_stages_ship_slice100_and_approve_commits` | COVERED |
| Chunk boundary 500 | `CmoMarkdownPlatformImportTests::ChunkPlatforms_with_501_rows_produces_two_batches_at_chunk_size_500` | COVERED |
| Fitting quarantine JSON | `CmoMarkdownPlatformImportTests::ProposePlatformsFromMarkdown_emits_fitting_quarantine_json_for_orphan_weapon` | COVERED |
| Baltic regression preserved | `CmoMarkdownPlatformImportTests::ProposePlatformsFromMarkdown_stages_baltic_fixture_with_mount_batches` | COVERED |