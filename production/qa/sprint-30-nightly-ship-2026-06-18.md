# Sprint 30 — Nightly ship.md Approve at Scale Evidence (S30-04)

**Date:** 2026-06-18  
**Story:** S30-04 — Nightly ship.md approve at scale  
**Scripts:** `tools/cmo-nightly-import.sh` (propose), `tools/cmo-nightly-approve.sh` (approve)

## Verdict: **PASS** (full `ship.md` off-CI; CI stays curated `ship-slice-100` + `MAX_RECORDS`)

| AC | Evidence | Result |
|----|----------|--------|
| Off-CI nightly → full `ship.md` propose → `ApproveBatch` | Full run below (4844 platforms, 10 platform batches) | **PASS** |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only; no direct SQLite writes | **PASS** |
| Not wired into `dotnet test` CI | Scripts under `tools/`; tests use curated fixtures + caps | **PASS** |
| Full 7208-record `sensor.md` stays off-CI | CI filter uses `--max-records` only | **PASS** |
| WriteGate regression PASS on curated fixtures | Data 182/182; Cli 8/8; sln 897/897 | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff` empty | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

**Data.Tests (filtered):** 182/182 PASS  
**Cli.Tests (filtered):** 8/8 PASS  
**Solution:** 897/897 PASS  
**ReplayGoldenSuite:** 6/6 PASS

### Curated propose→approve round-trip (`ship-slice-100.md`, CI-safe)

```bash
PLATFORM_MD=tools/cmano-db-crawler/fixtures/ship-slice-100.md \
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity platform --dry-run
```

| Field | Value |
|-------|-------|
| `batchId` | `batch-platform-12-0` |
| `parsedCount` | 12 |
| `batchCount` | 1 |

### Full off-CI nightly run (`ship.md`, 4844 records)

```bash
./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity platform \
  --release-version nightly-ship-s30-04-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/ship.md` |
| `parsedCount` | 4844 |
| Platform batches | 10 (`batch-platform-500-0` × 9 + `batch-platform-344-0`) |
| Total staged batches | 30 (platform + mount + loadout per chunk) |
| `releaseVersion` | `nightly-ship-s30-04-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
| `quarantinedCount` | 25239 *(orphan fittings — expected for full corpus without weapon cross-ref)* |

**Artifacts:**
- `scratch/nightly-cmo-20260618/platform-propose.json`
- `scratch/nightly-cmo-20260618/platform-approve-summary.json`
- `scratch/nightly-cmo-20260618/nightly-approve-summary.json`
- `scratch/nightly-cmo-20260618/platform-quarantine.json`

## Snapshot hash pinning approach

1. **Propose:** `catalog_import_markdown` stages rows in scratch DB via `CatalogWriteGate.Propose*Batch` (propose-only default).
2. **Approve:** `tools/cmo-nightly-approve.sh` invokes `catalog_write_approve` per batch ID from `*-propose.json`.
3. **Bind:** `CatalogWriteApproveCommand` calls `CatalogSnapshotBinder.BindAfterApprove` → `DbSnapshotStore.RecordRelease` with `content_hash_sha256`, `snapshot_id`, and `release_version`.
4. **Evidence:** `nightly-approve-summary.json` aggregates pinned hashes for curator sign-off.

## Scale metadata test (CI-safe, no full import)

`CmoMarkdownPlatformImportTests::Reference_ship_markdown_parses_4844_records_for_off_ci_nightly_scale` validates:
- `ResolveReferenceShipMarkdownPath()` → 4844 platform headings
- Chunk 500/batch → 10 batches (9×500 + 1×344)
- Parse-only; no SQLite staging in `dotnet test`

## Partial failure recovery

- Approve script processes batches independently; a failed batch leaves prior approved commits intact.
- Re-run import to refresh `*-propose.json`; re-run approve for remaining/failed batch IDs.
- Quarantine JSON per entity supports triage without committing rejected rows.
- `platform-approve-<batchId>.json` captures per-batch CLI output for post-mortem.

## S29-03 carryover

S30-04 scales the S29-03 curated path to full `ship.md`. No `CatalogWriteGate` signature changes; `DelegationBridge.cs` untouched. Nightly scripts already defaulted to `ship.md` via `PLATFORM_MD`; S30-04 adds scale metadata test + evidence for the full corpus run.