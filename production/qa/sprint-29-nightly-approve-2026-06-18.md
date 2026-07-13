# Sprint 29 — Nightly Corpus Approve Workflow Evidence (S29-03)

**Date:** 2026-06-18  
**Story:** S29-03 — Nightly corpus approve workflow  
**Scripts:** `tools/cmo-nightly-import.sh` (propose), `tools/cmo-nightly-approve.sh` (approve)

## Verdict: **PASS** (local curated gate with `MAX_RECORDS=12`; full ship.md run is off-CI)

| AC | Evidence | Result |
|----|----------|--------|
| Nightly propose→approve completes off-CI | Curated platform slice run below | **PASS** |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only; no direct SQLite writes | **PASS** |
| Not wired into `dotnet test` CI | Scripts under `tools/`; tests use curated fixtures + caps | **PASS** |
| Full 7208-record sensor stays off-CI | CI filter uses `--max-records` only | **PASS** |
| WriteGate regression PASS on curated fixtures | Data 174/174; Cli 8/8 | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff` empty | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform|CatalogImport|Snapshot" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only --dry-run
```

**Data.Tests:** 174/174 PASS  
**Cli.Tests:** 8/8 PASS

### Curated propose→approve round-trip (`ship-slice-100.md`)

```bash
PLATFORM_MD=tools/cmano-db-crawler/fixtures/ship-slice-100.md \
  MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only

./tools/cmo-nightly-approve.sh --entity platform \
  --release-version nightly-platform-s29-03-20260618
```

| Field | Value |
|-------|-------|
| `batchId` | `batch-platform-12-0` |
| `snapshotId` | `baltic_patrol` |
| `releaseVersion` | `nightly-platform-s29-03-20260618` |
| `contentHashSha256` | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
| `parsedCount` | 12 |
| `batchCount` | 1 |

**Artifacts:**
- `scratch/nightly-cmo-20260618/platform-propose.json`
- `scratch/nightly-cmo-20260618/platform-approve-batch-platform-12-0.json`
- `scratch/nightly-cmo-20260618/platform-approve-summary.json`
- `scratch/nightly-cmo-20260618/nightly-approve-summary.json`

## Snapshot hash pinning approach

1. **Propose:** `catalog_import_markdown` stages rows in scratch DB via `CatalogWriteGate.Propose*Batch` (propose-only default).
2. **Approve:** `tools/cmo-nightly-approve.sh` invokes `catalog_write_approve` per batch ID from `*-propose.json`.
3. **Bind:** `CatalogWriteApproveCommand` calls `CatalogSnapshotBinder.BindAfterApprove` → `DbSnapshotStore.RecordRelease` with `content_hash_sha256`, `snapshot_id`, and `release_version`.
4. **Evidence:** `nightly-approve-summary.json` aggregates pinned hashes for curator sign-off.

## Production nightly

Unset `MAX_RECORDS` for full corpora:

- `docs/reference/cmano-db/sensor.md` (7208) — propose-only; approve via `cmo-nightly-approve.sh`
- `docs/reference/cmano-db/weapon.md`
- `docs/reference/cmano-db/ship.md` (4844 platforms)

```bash
./tools/cmo-nightly-import.sh --entity all --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity all --release-version nightly-corpus-$(date -u +%Y%m%d)
```

## Partial failure recovery

- Approve script processes batches independently; a failed batch leaves prior approved commits intact.
- Re-run import to refresh `*-propose.json`; re-run approve for remaining/failed batch IDs.
- Quarantine JSON per entity supports triage without committing rejected rows.
- `platform-approve-<batchId>.json` captures per-batch CLI output for post-mortem.

## Merge-conflict note (S29-02)

**Low risk.** S29-02 (TL export Phase 1–2) targets migration `007` / `catalog_snapshot.branch` metadata. S29-03 adds off-CI shell tooling and WriteGate regression tests only — no `CatalogWriteGate` signature changes and no migration edits. Shared touch surface is limited to `DbSnapshotStore.RecordRelease` read paths in evidence (extend-only, already used by `CatalogWriteApproveCommand`).