# S31-11 story-done — Entity Corpus Nightly Approve at Scale

**Story:** `production/epics/sprint-31-corpus-approve-complete/story-031-11-entity-approve-scale.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/entity-approve-scale`  
**Prerequisite:** S30-11 COMPLETE (`production/agentic/stacks/sprint30/S30-11-DONE.md`)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Off-CI nightly approve per entity domain (aircraft, facility, submarine) | Full runs: 7387 + 4511 + 732 platforms | **PASS** |
| `RecordRelease` + pinned snapshot hash per domain | `*-approve-summary.json` per entity | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only | **PASS** |
| Not wired into `dotnet test` CI | curated `*-slice-100` fixtures + `MAX_RECORDS` | **PASS** |
| Full corpora remain nightly-only | no full entity `.md` in CI filter | **PASS** |
| WriteGate regression PASS on curated per-domain fixtures | Data 196/196; Cli 11/11 | **PASS** |
| Combined evidence doc | `production/qa/sprint-31-nightly-entity-2026-06-18.md` | **PASS** |
| ZERO touch `DelegationBridge` | empty diff | **PASS** |

## Deliverables

- `tools/cmo-nightly-import.sh` / `tools/cmo-nightly-approve.sh` — existing S30-11 `--entity aircraft|submarine|facility` path exercised at full corpus scale
- Evidence: `production/qa/sprint-31-nightly-entity-2026-06-18.md`

## Pinned snapshot hashes (full corpora nightly)

### Aircraft (`aircraft.md`)

| Field | Value |
|-------|-------|
| `parsedCount` | 7387 |
| Propose batches | 15 (14×500 + 387) |
| Unique approve batches | 2 (`batch-platform-500-0`, `batch-platform-387-0`) |
| `releaseVersion` | `nightly-aircraft-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |
| `quarantinedCount` | 0 |

### Submarine (`submarine.md`)

| Field | Value |
|-------|-------|
| `parsedCount` | 732 |
| Propose batches | 6 |
| Unique approve batches | 6 |
| `releaseVersion` | `nightly-submarine-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |
| `quarantinedCount` | 3218 |

### Facility (`facility.md`)

| Field | Value |
|-------|-------|
| `parsedCount` | 4511 |
| Propose batches | 28 |
| Unique approve batches | 20 |
| `releaseVersion` | `nightly-facility-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |
| `quarantinedCount` | 5631 |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only --dry-run
dotnet test src/ProjectAegis.Data.Tests \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests \
  --filter "CatalogImport|Platform" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test-criterion traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| Curated entity slice propose→approve per domain | `CmoMarkdownEntitySliceImportTests` (S30-11) | COVERED |
| CLI entity slice smoke | `CatalogImportMarkdownCommandTests` (3 domain tests) | COVERED |
| Golden hash pin per domain | `CatalogSortKeyGoldenHashes` + reimport tests | COVERED |
| Off-CI full corpus runs | `production/qa/sprint-31-nightly-entity-2026-06-18.md` | COVERED |

## Test counts (2026-06-18)

| Suite | Result |
|-------|--------|
| Data.Tests (filtered) | **196/196 PASS** |
| Cli.Tests (filtered) | **11/11 PASS** |

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only; no signature changes)
- `DelegationBridge.cs`

## Unblocks

- **Sprint 31 corpus gate** — platform entity family full nightly approve complete (ship S30-04 + aircraft/submarine/facility S31-11)
- **S31-12** — CI/local gate refresh