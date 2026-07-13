# S30-04 story-done — Nightly ship.md Approve at Scale

**Story:** `production/epics/sprint-30-corpus-approve-scale/story-030-04-ship-approve-scale.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/ship-approve-scale`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Off-CI nightly → full `ship.md` propose → `ApproveBatch` | Full run: 4844 platforms, 10 platform batches | COVERED |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | COVERED |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only | COVERED |
| Not wired into `dotnet test` CI | curated fixtures + `--max-records` | COVERED |
| Full 7208-record `sensor.md` off-CI | no sensor.md in CI filter | COVERED |
| WriteGate regression PASS | Data 182/182; Cli 8/8; sln 897/897 | COVERED |
| Evidence doc | `production/qa/sprint-30-nightly-ship-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge` | empty diff | COVERED |

## Deliverables

- `CmoMarkdownImporter.ResolveReferenceShipMarkdownPath()` — full corpus path helper
- `CmoMarkdownPlatformImportTests::Reference_ship_markdown_parses_4844_records_for_off_ci_nightly_scale` — scale metadata (parse-only, CI-safe)
- `tools/cmo-nightly-import.sh` / `tools/cmo-nightly-approve.sh` — S30-04 header annotations (S28-02/S29-03 foundation unchanged)
- Evidence: `production/qa/sprint-30-nightly-ship-2026-06-18.md`

## Pinned snapshot hash (full ship.md nightly)

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/ship.md` |
| `parsedCount` | 4844 |
| Platform batches | 10 (chunk 500/batch) |
| `releaseVersion` | `nightly-ship-s30-04-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests \
  --filter "CatalogImport|Platform" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
PLATFORM_MD=tools/cmano-db-crawler/fixtures/ship-slice-100.md MAX_RECORDS=12 \
  ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity platform --dry-run
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test-criterion traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| Full ship.md scale metadata | `Reference_ship_markdown_parses_4844_records_for_off_ci_nightly_scale` | COVERED |
| Curated propose→approve + hash pin | `CmoNightlyApproveWorkflowTests` (S29-03 carryover) | COVERED |
| Multi-batch approve path | `Nightly_multi_batch_platform_approve_commits_all_chunks_via_WriteGate` | COVERED |
| CLI import→approve round-trip | `CatalogWriteCommandTests` | COVERED |
| Off-CI full corpus run | `production/qa/sprint-30-nightly-ship-2026-06-18.md` | COVERED |

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only; no signature changes)
- `DelegationBridge.cs`

## Unblocks

- **S30-11** — CMO entity nightly slices (`aircraft` / `submarine` / `facility`)
- **Sprint 30 corpus gate** — full `ship.md` approve at scale complete