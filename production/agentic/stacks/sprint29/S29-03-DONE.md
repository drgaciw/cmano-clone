# S29-03 story-done — Nightly Corpus Approve Workflow

**Story:** `production/epics/sprint-29-corpus-approve/story-029-03-nightly-approve.md`  
**Status:** Complete  
**Date:** 2026-06-18

## Deliverables

- `tools/cmo-nightly-approve.sh` — off-CI companion: reads `*-propose.json`, runs `catalog_write_approve` per batch, writes `nightly-approve-summary.json`
- `tools/cmo-nightly-import.sh` — footer references approve companion script
- `CmoNightlyApproveWorkflowTests` — curated propose→approve→`RecordRelease` regression (3 tests)
- `CatalogWriteCommandTests::catalog_import_markdown_then_write_approve_records_snapshot_hash_for_platform_slice` — CLI round-trip
- Evidence: `production/qa/sprint-29-nightly-approve-2026-06-18.md`

## Pinned snapshot hash (curated gate)

| Field | Value |
|-------|-------|
| Fixture | `tools/cmano-db-crawler/fixtures/ship-slice-100.md` |
| `batchId` | `batch-platform-12-0` |
| `snapshotId` | `baltic_patrol` |
| `releaseVersion` | `nightly-platform-s29-03-20260618` |
| `contentHashSha256` | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal   # 174/174 PASS
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal                                   # 8/8 PASS
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only --dry-run
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
```

## Acceptance criteria

| AC | Test / Evidence | Verdict |
|----|-----------------|---------|
| Nightly propose→approve completes off-CI | `cmo-nightly-approve.sh` + QA evidence | **PASS** |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | **PASS** |
| All commits via `CatalogWriteGate` | `catalog_write_approve` only | **PASS** |
| Not in `dotnet test` CI | `tools/` scripts + curated test caps | **PASS** |
| Full 7208-record sensor off-CI | no sensor.md in test filter | **PASS** |
| WriteGate regression PASS | 174/174 Data; 8/8 Cli | **PASS** |
| ZERO touch DelegationBridge | empty `git diff` | **PASS** |

## Test-criterion traceability

| Criterion | Test | Status |
|-----------|------|--------|
| Platform slice approve + hash pin | `CmoNightlyApproveWorkflowTests::Nightly_platform_slice_propose_approve_records_pinned_snapshot_hash` | COVERED |
| Multi-batch approve path | `CmoNightlyApproveWorkflowTests::Nightly_multi_batch_platform_approve_commits_all_chunks_via_WriteGate` | COVERED |
| Sensor nightly approve + release row | `CmoNightlyApproveWorkflowTests::Nightly_sensor_slice_propose_approve_records_release_row` | COVERED |
| CLI import→approve round-trip | `CatalogWriteCommandTests::catalog_import_markdown_then_write_approve_records_snapshot_hash_for_platform_slice` | COVERED |
| Existing WriteGate / snapshot regressions | filtered suites (174 tests) | COVERED |

## S29-02 merge-conflict risk

**Low.** S29-02 scopes migration `007` / export manifest `tlTier`. S29-03 does not edit migrations or `CatalogWriteGate` signatures. Only shared artifact is `DbSnapshotStore.RecordRelease` (read-only in evidence path).