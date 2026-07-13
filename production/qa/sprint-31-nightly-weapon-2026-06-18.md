# Sprint 31 — Nightly weapon.md Approve at Scale Evidence (S31-10)

**Date:** 2026-06-18  
**Story:** S31-10 — Nightly weapon.md approve at scale  
**Scripts:** `tools/cmo-nightly-import.sh` (propose), `tools/cmo-nightly-approve.sh` (approve)

## Verdict: **PASS** (full `weapon.md` off-CI; CI stays curated `weapon-slice-50` + `MAX_RECORDS`)

| AC | Evidence | Result |
|----|----------|--------|
| Off-CI nightly → full `weapon.md` propose → `ApproveBatch` | Full run below (4403 weapons, 9 propose batches) | **PASS** |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | **PASS** |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only; no direct SQLite writes | **PASS** |
| Not wired into `dotnet test` CI | Scripts under `tools/`; tests use curated fixtures + caps | **PASS** |
| WriteGate regression PASS on curated fixtures | Data 197/197; Cli 11/11 | **PASS** |
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

**Data.Tests (filtered):** 197/197 PASS  
**Cli.Tests (filtered):** 11/11 PASS  
**ReplayGoldenSuite:** 6/6 PASS

### Curated propose→approve round-trip (CI-safe)

```bash
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only --dry-run
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity weapon --dry-run
```

| Field | Value |
|-------|-------|
| `batchId` | `batch-weapon-12-0` |
| `parsedCount` | 12 |
| `batchCount` | 1 |
| `quarantinedCount` | 0 |

### Full off-CI nightly run (`weapon.md`, 4403 records)

```bash
./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only
./tools/cmo-nightly-approve.sh --entity weapon \
  --release-version nightly-weapon-s31-10-20260618
```

Weapon-only isolated scratch (clean DB evidence):

```bash
SCRATCH_DIR=scratch/nightly-weapon-s31-10-20260618
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_import_markdown --db "$SCRATCH_DIR/catalog-proposed.db" \
  --markdown docs/reference/cmano-db/weapon.md --entity weapon --chunk-size 500 \
  --report-out "$SCRATCH_DIR/weapon-quarantine.json" > "$SCRATCH_DIR/weapon-propose.json"
./tools/cmo-nightly-approve.sh --entity weapon \
  --scratch-dir "$SCRATCH_DIR" \
  --release-version nightly-weapon-s31-10-20260618
```

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/weapon.md` |
| `parsedCount` | 4403 |
| Propose batches | 9 (8×500 + 1×403; chunk 500/batch) |
| Unique approve batch IDs | 2 (`batch-weapon-500-0` accumulates 8 chunks; `batch-weapon-403-0` tail) |
| `releaseVersion` | `nightly-weapon-s31-10-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (batch-weapon-500-0) | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
| `contentHashSha256` (batch-weapon-403-0) | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
| `quarantinedCount` | 0 |
| Committed `weapon_catalog` rows | 4403 |

**Artifacts:**
- `scratch/nightly-weapon-s31-10-20260618/weapon-propose.json`
- `scratch/nightly-weapon-s31-10-20260618/weapon-approve-summary.json`
- `scratch/nightly-weapon-s31-10-20260618/nightly-approve-summary.json`
- `scratch/nightly-weapon-s31-10-20260618/weapon-quarantine.json`
- `scratch/nightly-weapon-s31-10-20260618/weapon-approve-batch-weapon-500-0.json`
- `scratch/nightly-weapon-s31-10-20260618/weapon-approve-batch-weapon-403-0.json`

## Snapshot hash pinning approach

1. **Propose:** `catalog_import_markdown` stages rows in scratch DB via `CatalogWriteGate.ProposeWeaponBatch` (propose-only default).
2. **Approve:** `tools/cmo-nightly-approve.sh` invokes `catalog_write_approve` per batch ID from `*-propose.json`.
3. **Bind:** `CatalogWriteApproveCommand` calls `CatalogSnapshotBinder.BindAfterApprove` → `DbSnapshotStore.RecordRelease` with `content_hash_sha256`, `snapshot_id`, and `release_version`.
4. **Evidence:** `nightly-approve-summary.json` aggregates pinned hashes for curator sign-off.

## Batch ID note (weapon domain)

Weapon propose batches share `FixedCatalogClock(0)` tick suffix (`batch-weapon-500-0`, `batch-weapon-403-0`). Staging rows accumulate under composite key `(batch_id, weapon_id)`; approve resolves to two unique batch IDs covering all 4403 parsed records. Mirrors S31-02 sensor and S30-04 ship patterns where duplicate batch IDs collapse at approve time.

## Scale metadata test (CI-safe, no full import)

`CmoMarkdownWeaponImportTests::Reference_weapon_markdown_parses_4403_records_for_off_ci_nightly_scale` validates:
- `ResolveReferenceWeaponMarkdownPath()` → 4403 weapon headings
- Chunk 500/batch → 9 batches (8×500 + 1×403)
- Parse-only; no SQLite staging in `dotnet test`

## Partial failure recovery

- Approve script processes batches independently; a failed batch leaves prior approved commits intact.
- Re-run import to refresh `*-propose.json`; re-run approve for remaining/failed batch IDs.
- Quarantine JSON per entity supports triage without committing rejected rows.
- `weapon-approve-<batchId>.json` captures per-batch CLI output for post-mortem.

## S31-02 carryover

S31-10 scales the S26-02 curated `weapon-slice-50` path to full `weapon.md`. No `CatalogWriteGate` signature changes; `DelegationBridge.cs` untouched. Nightly scripts already supported `--entity weapon`; S31-10 adds off-CI scale evidence for the full 4403-record corpus run.