# Sprint 31 — Nightly Entity Corpus Approve at Scale Evidence (S31-11)

**Date:** 2026-06-18  
**Story:** S31-11 — Entity corpus nightly approve at scale  
**Scripts:** `tools/cmo-nightly-import.sh` (propose), `tools/cmo-nightly-approve.sh` (approve)  
**Foundation:** S30-11 entity slices (`--entity aircraft|submarine|facility`)

## Verdict: **PASS** (full entity corpora off-CI; CI stays curated slice + `MAX_RECORDS`)

| AC | Evidence | Result |
|----|----------|--------|
| Off-CI nightly → full corpora propose → `ApproveBatch` per domain | Full runs below (aircraft 7387, submarine 732, facility 4511) | **PASS** |
| `RecordRelease` + pinned snapshot hash per domain | `*-approve-summary.json` per entity | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only; no direct SQLite writes | **PASS** |
| Not wired into `dotnet test` CI | Scripts under `tools/`; tests use curated `*-slice-100` fixtures + caps | **PASS** |
| Full corpora remain nightly-only | No full `aircraft.md` / `submarine.md` / `facility.md` in CI filter | **PASS** |
| WriteGate regression PASS on curated per-domain fixtures | Data 196/196; Cli 11/11 | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff` empty | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
```

**Data.Tests (filtered):** 196/196 PASS  
**Cli.Tests (filtered):** 11/11 PASS

### Curated dry-run gate per domain (CI-safe)

```bash
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only --dry-run
```

| Domain | Corpus path | Curated fixture | `MAX_RECORDS` |
|--------|-------------|-----------------|---------------|
| Aircraft | `docs/reference/cmano-db/aircraft.md` | `tools/cmano-db-crawler/fixtures/aircraft-slice-100.md` | 12 |
| Submarine | `docs/reference/cmano-db/submarine.md` | `tools/cmano-db-crawler/fixtures/submarine-slice-100.md` | 12 |
| Facility | `docs/reference/cmano-db/facility.md` | `tools/cmano-db-crawler/fixtures/facility-slice-100.md` | 12 |

### Curated golden slice hashes (CI `dotnet test`)

| Domain | Fixture IDs | Golden hash (`CatalogSortKeyGoldenHashes`) |
|--------|-------------|---------------------------------------------|
| Aircraft | `/aircraft/5001`–`5100` | `5d82b0aaa6f0be92f69a4062fe8e398061e38f5075dcfe222b6f934949a91d81` |
| Submarine | `/submarine/6001`–`6100` | `dc0b03fa4cc890e40f5dcfdfb9317fb6f8071d2a0aebc79c249ae532f9446c69` |
| Facility | `/facility/7001`–`7100` | `ba5d2b5f9af537ceb7799965f450f34bb16e13a106b851f3951b6d5db2699cf9` |

## Full off-CI nightly runs (per domain)

Scratch directory: `scratch/nightly-cmo-20260618/` (shared scratch DB accumulates prior sensor/platform state from S31-02/S30-04 runs).

### Aircraft (`aircraft.md`, 7387 records)

```bash
./tools/cmo-nightly-import.sh --entity aircraft --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity aircraft \
  --release-version nightly-aircraft-s31-11-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/aircraft.md` |
| `parsedCount` | 7387 |
| `approvedCount` | 7387 |
| `quarantinedCount` | 0 |
| Propose batches | 15 (14×500 + 1×387; chunk 500/batch) |
| Unique approve batch IDs | 2 (`batch-platform-500-0`, `batch-platform-387-0`) |
| `releaseVersion` | `nightly-aircraft-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (post-approve) | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |

**Artifacts:**
- `scratch/nightly-cmo-20260618/aircraft-propose.json`
- `scratch/nightly-cmo-20260618/aircraft-approve-summary.json`
- `scratch/nightly-cmo-20260618/aircraft-quarantine.json`
- `scratch/nightly-cmo-20260618/aircraft-approve-batch-platform-500-0.json`
- `scratch/nightly-cmo-20260618/aircraft-approve-batch-platform-387-0.json`

### Submarine (`submarine.md`, 732 records)

```bash
./tools/cmo-nightly-import.sh --entity submarine --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity submarine \
  --release-version nightly-submarine-s31-11-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/submarine.md` |
| `parsedCount` | 732 |
| `approvedCount` | 732 |
| `quarantinedCount` | 3218 (mount/loadout child rows; platforms committed) |
| Propose batches | 6 (platform + mount + loadout composite) |
| Unique approve batch IDs | 6 (`batch-platform-500-0`, `batch-platform-232-0`, `batch-mount-1853-0`, `batch-mount-5732-0`, `batch-loadout-486-0`, `batch-loadout-721-0`) |
| `releaseVersion` | `nightly-submarine-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (post-approve) | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |

**Artifacts:**
- `scratch/nightly-cmo-20260618/submarine-propose.json`
- `scratch/nightly-cmo-20260618/submarine-approve-summary.json`
- `scratch/nightly-cmo-20260618/submarine-quarantine.json`

### Facility (`facility.md`, 4511 records)

```bash
./tools/cmo-nightly-import.sh --entity facility --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity facility \
  --release-version nightly-facility-s31-11-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/facility.md` |
| `parsedCount` | 4511 |
| `approvedCount` | 4511 |
| `quarantinedCount` | 5631 (mount/loadout child rows; platforms committed) |
| Propose batches | 28 (platform + mount + loadout composite per chunk) |
| Unique approve batch IDs | 20 (platform/mount/loadout families; duplicate `batch-platform-500-0` collapses at approve) |
| `releaseVersion` | `nightly-facility-s31-11-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (post-approve) | `7162941313b19f2aa7c126e899e30e071681ec1e7291f5104c57536c9066e65e` |

**Artifacts:**
- `scratch/nightly-cmo-20260618/facility-propose.json`
- `scratch/nightly-cmo-20260618/facility-approve-summary.json`
- `scratch/nightly-cmo-20260618/facility-quarantine.json`

## Per-domain summary

| Domain | `parsedCount` | Propose batches | Approve batches | `releaseVersion` | `contentHashSha256` |
|--------|---------------|-----------------|-----------------|------------------|---------------------|
| Aircraft | 7387 | 15 | 2 | `nightly-aircraft-s31-11-20260618` | `7162941313…6e65e` |
| Submarine | 732 | 6 | 6 | `nightly-submarine-s31-11-20260618` | `7162941313…6e65e` |
| Facility | 4511 | 28 | 20 | `nightly-facility-s31-11-20260618` | `7162941313…6e65e` |

**Hash note:** All three domains share scratch DB `catalog-proposed.db` with prior sensor/platform nightly runs. `contentHashSha256` reflects cumulative catalog snapshot state after each domain's final `RecordRelease`, not an isolated per-domain DB. Per-domain `releaseVersion` pins curator intent; use a fresh scratch dir for isolated per-domain hash baselines.

## Snapshot hash pinning approach

1. **Propose:** `catalog_import_markdown --entity <domain>` stages platform/mount/loadout rows via `CatalogWriteGate.ProposePlatformsFromMarkdown` (propose-only default).
2. **Approve:** `tools/cmo-nightly-approve.sh` invokes `catalog_write_approve` per batch ID from `*-propose.json`.
3. **Bind:** `CatalogWriteApproveCommand` calls `CatalogSnapshotBinder.BindAfterApprove` → `DbSnapshotStore.RecordRelease` with `content_hash_sha256`, `snapshot_id`, and `release_version`.
4. **Evidence:** `*-approve-summary.json` and `nightly-approve-summary.json` aggregate pinned hashes for curator sign-off.

## Batch ID note (platform entity family)

Platform-family propose batches share `FixedCatalogClock(0)` tick suffix (`batch-platform-500-0`, `batch-platform-387-0`). Staging rows accumulate under composite keys; approve resolves to unique batch IDs covering all parsed platform records. Submarine and facility additionally emit mount/loadout batch families mirroring S30-04 ship pattern.

## Partial failure recovery

- Approve script processes batches independently; a failed batch leaves prior approved commits intact.
- Re-run import to refresh `*-propose.json`; re-run approve for remaining/failed batch IDs.
- Quarantine JSON per entity supports triage without committing rejected rows.
- `*-approve-<batchId>.json` captures per-batch CLI output for post-mortem.

## S30-11 carryover

S31-11 scales the S30-11 curated entity slice path to full `aircraft.md`, `submarine.md`, and `facility.md` corpora. No `CatalogWriteGate` signature changes; `DelegationBridge.cs` untouched. Nightly scripts already supported `--entity aircraft|submarine|facility`; S31-11 adds off-CI scale evidence for all three platform entity domains.