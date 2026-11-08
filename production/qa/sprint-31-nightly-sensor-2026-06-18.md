# Sprint 31 — Nightly sensor.md Approve at Scale Evidence (S31-02)

**Date:** 2026-06-18  
**Story:** S31-02 — Nightly sensor.md approve at scale  
**Scripts:** `tools/cmo-nightly-import.sh` (propose), `tools/cmo-nightly-approve.sh` (approve)

## Verdict: **PASS** (full `sensor.md` off-CI; CI stays curated slice + `MAX_RECORDS`)

| AC | Evidence | Result |
|----|----------|--------|
| Off-CI nightly → full `sensor.md` propose → `ApproveBatch` | Full run below (7208 sensors, 15 propose batches) | **PASS** |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only; no direct SQLite writes | **PASS** |
| Not wired into `dotnet test` CI | Scripts under `tools/`; tests use curated fixtures + caps | **PASS** |
| WriteGate regression PASS on curated fixtures | Data 194/194; Cli 11/11 | **PASS** |
| ReplayGolden 6/6 unchanged on default path | `ReplayGoldenSuiteTests` | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff` empty | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

**Data.Tests (filtered):** 194/194 PASS  
**Cli.Tests (filtered):** 11/11 PASS  
**ReplayGoldenSuite:** 6/6 PASS

### Curated propose→approve round-trip (CI-safe)

```bash
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity sensor --dry-run
```

| Field | Value |
|-------|-------|
| `batchId` | `batch-12-0` |
| `parsedCount` | 12 |
| `batchCount` | 1 |
| `quarantinedCount` | 0 |

### Full off-CI nightly run (`sensor.md`, 7208 records)

```bash
./tools/cmo-nightly-import.sh --entity sensor --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity sensor \
  --release-version nightly-sensor-s31-02-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/sensor.md` |
| `parsedCount` | 7208 |
| Propose batches | 15 (14×500 + 1×208; chunk 500/batch) |
| Unique approve batch IDs | 2 (`batch-500-0` accumulates 14 chunks; `batch-208-0` tail) |
| `releaseVersion` | `nightly-sensor-s31-02-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (batch-500-0) | `f4342023b96528575c39fe44c21e19dac88606bf3d29f184a0ddedaa883355c9` |
| `contentHashSha256` (batch-208-0) | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |
| `quarantinedCount` | 0 |

**Artifacts:**
- `scratch/nightly-cmo-20260618/sensor-propose.json`
- `scratch/nightly-cmo-20260618/sensor-approve-summary.json`
- `scratch/nightly-cmo-20260618/nightly-approve-summary.json`
- `scratch/nightly-cmo-20260618/sensor-quarantine.json`
- `scratch/nightly-cmo-20260618/sensor-approve-batch-500-0.json`
- `scratch/nightly-cmo-20260618/sensor-approve-batch-208-0.json`

## Snapshot hash pinning approach

1. **Propose:** `catalog_import_markdown` stages rows in scratch DB via `CatalogWriteGate.ProposeSensorBatch` (propose-only default).
2. **Approve:** `tools/cmo-nightly-approve.sh` invokes `catalog_write_approve` per batch ID from `*-propose.json`.
3. **Bind:** `CatalogWriteApproveCommand` calls `CatalogSnapshotBinder.BindAfterApprove` → `DbSnapshotStore.RecordRelease` with `content_hash_sha256`, `snapshot_id`, and `release_version`.
4. **Evidence:** `nightly-approve-summary.json` aggregates pinned hashes for curator sign-off.

## Batch ID note (sensor domain)

Sensor propose batches share `FixedCatalogClock(0)` tick suffix (`batch-500-0`, `batch-208-0`). Staging rows accumulate under composite key `(batch_id, platform_id, sensor_id)`; approve resolves to two unique batch IDs covering all 7208 parsed records. Mirrors S30-04 ship pattern where duplicate platform batch IDs collapse at approve time.

## Partial failure recovery

- Approve script processes batches independently; a failed batch leaves prior approved commits intact.
- Re-run import to refresh `*-propose.json`; re-run approve for remaining/failed batch IDs.
- Quarantine JSON per entity supports triage without committing rejected rows.
- `sensor-approve-<batchId>.json` captures per-batch CLI output for post-mortem.

## S30-04 carryover

S31-02 scales the S29-03 curated path to full `sensor.md`. No `CatalogWriteGate` signature changes; `DelegationBridge.cs` untouched. Nightly scripts already supported `--entity sensor`; S31-02 adds off-CI scale evidence for the full 7208-record corpus run.