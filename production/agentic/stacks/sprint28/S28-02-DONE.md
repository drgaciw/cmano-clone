# S28-02 story-done — Nightly CMO corpus v2 platform slices

**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `tools/cmo-nightly-import.sh` — extended for platform v2 (sensor + weapon + platform)
  - `--entity sensor|weapon|platform|all` (default: all)
  - `--chunk-size`, `--max-records`, `--propose-only`, `--dry-run` CLI flags
  - Production platform path: `docs/reference/cmano-db/ship.md`
  - Curated CI gate fixture: `tools/cmano-db-crawler/fixtures/ship-slice-100.md` via `PLATFORM_MD` override
- Evidence: `production/qa/sprint-28-nightly-cmo-import-2026-06-18.md`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Platform entity slices in nightly script | `--entity platform` + `ship.md` import | COVERED |
| Chunk 500/batch | default + `--chunk-size` flag | COVERED |
| Propose-only + quarantine JSON | `--report-out` per entity | COVERED |
| No direct SQLite writes | staged scratch DB only | COVERED |
| CI isolation preserved | curated fixtures + `--max-records`; script off-CI | COVERED |
| Full 7208 sensor off-CI | unchanged from S27-02 | COVERED |
| Sensor + weapon v1 intact | `--entity all` runs all three | COVERED |
| ZERO touch DelegationBridge | no adapter changes | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|Platform|CatalogImport" -v minimal   # 143/143 PASS
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal               # 7/7 PASS
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh                 # PASS
```