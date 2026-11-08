# S31-02 story-done — Nightly sensor.md Approve at Scale

**Story:** `production/epics/sprint-31-corpus-approve-complete/story-031-02-nightly-sensor-approve.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/nightly-sensor-approve`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Off-CI nightly → full `sensor.md` propose → `ApproveBatch` | Full run: 7208 sensors, 15 propose batches | COVERED |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | COVERED |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only | COVERED |
| Not wired into `dotnet test` CI | curated fixtures + `--max-records` | COVERED |
| WriteGate regression PASS | Data 194/194; Cli 11/11 | COVERED |
| ReplayGolden 6/6 unchanged | `ReplayGoldenSuiteTests` 6/6 | COVERED |
| Evidence doc | `production/qa/sprint-31-nightly-sensor-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge` | empty diff | COVERED |

## Deliverables

- `tools/cmo-nightly-import.sh` / `tools/cmo-nightly-approve.sh` — existing `--entity sensor` path exercised at full corpus scale
- Evidence: `production/qa/sprint-31-nightly-sensor-2026-06-18.md`

## Pinned snapshot hash (full sensor.md nightly)

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/sensor.md` |
| `parsedCount` | 7208 |
| Propose batches | 15 (chunk 500/batch: 14×500 + 1×208) |
| Unique approve batches | 2 (`batch-500-0`, `batch-208-0`) |
| `releaseVersion` | `nightly-sensor-s31-02-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (batch-500-0) | `f4342023b96528575c39fe44c21e19dac88606bf3d29f184a0ddedaa883355c9` |
| `contentHashSha256` (batch-208-0) | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |
| `quarantinedCount` | 0 |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests \
  --filter "CatalogImport|Platform" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only --dry-run
./tools/cmo-nightly-approve.sh --entity sensor --dry-run
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test-criterion traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| Curated propose→approve + hash pin | `CmoNightlyApproveWorkflowTests` (S29-03 carryover) | COVERED |
| Sensor markdown parse smoke | `Reference_sensor_markdown_subset_parses_without_node_toolchain` | COVERED |
| Bulk chunk propose path | `ProposeFromMarkdown_with_501_sensors_produces_two_propose_batches` | COVERED |
| Off-CI full corpus run | `production/qa/sprint-31-nightly-sensor-2026-06-18.md` | COVERED |

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only; no signature changes)
- `DelegationBridge.cs`

## Unblocks

- **S31-09** — Balance drift advisory on nightly approve summary
- **S31-10** — Full `weapon.md` nightly approve at scale
- **S31-11** — Entity corpus nightly approve at scale
- **Sprint 31 corpus gate** — full `sensor.md` approve at scale complete