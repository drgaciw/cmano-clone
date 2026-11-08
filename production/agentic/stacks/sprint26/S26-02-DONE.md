# S26-02 story-done evidence — CMO weapon markdown import CLI

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-02  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `CmoMarkdownImportEntity` enum — Sensor / Weapon / Platform routing
- `CmoMarkdownImportProposer.ProposeWeaponsFromMarkdown` + `ChunkWeapons`
- `CatalogImportMarkdownCommand` + `Program.cs` — `--entity weapon`, `--max-records`
- Fixture: `tools/cmano-db-crawler/fixtures/weapon-slice-50.md` (50 synthetic weapons)
- Tests:
  - `CmoMarkdownWeaponImportTests.cs` — E2E propose/approve + chunk boundary (501→2 batches)
  - `CatalogImportMarkdownCommandTests.catalog_import_markdown_weapon_entity_proposes_fifty_weapons`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdownWeaponImportTests" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "catalog_import_markdown_weapon" -v minimal
# Data: 2/2 PASS; CLI: 1/1 PASS
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| ≥50 weapons staged+approved from curated slice | `weapon-slice-50.md` → 50 parsed, 50 approved, 50 in `weapon_catalog` | **PASS** |
| `catalog_import_markdown --entity weapon` CLI | `CatalogImportMarkdownCommandTests` JSON `entity: weapon`, `parsedCount: 50` | **PASS** |
| Chunk policy 500/batch | `ChunkWeapons_with_501_rows_produces_two_batches_at_chunk_size_500` | **PASS** |
| No direct SQLite writes | All commits via `CatalogWriteGate.ApproveBatch` | **PASS** |
| Quarantine JSON path | Existing sensor quarantine tests unchanged; weapon path uses write gate only | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| 50-weapon E2E import | `CmoMarkdownWeaponImportTests::ProposeWeaponsFromMarkdown_stages_slice50_and_approve_commits` | COVERED |
| Chunk boundary 500 | `CmoMarkdownWeaponImportTests::ChunkWeapons_with_501_rows_produces_two_batches_at_chunk_size_500` | COVERED |
| CLI weapon entity | `CatalogImportMarkdownCommandTests::catalog_import_markdown_weapon_entity_proposes_fifty_weapons` | COVERED |