# S31-10 story-done — Nightly weapon.md Approve at Scale

**Story:** `production/epics/sprint-31-corpus-approve-complete/story-031-10-weapon-approve-scale.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/weapon-approve-scale`

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Off-CI nightly → full `weapon.md` propose → `ApproveBatch` | Full run: 4403 weapons, 9 propose batches | COVERED |
| `RecordRelease` + pinned snapshot hash | `nightly-approve-summary.json` | COVERED |
| All commits via `CatalogWriteGate.ApproveBatch` | `catalog_write_approve` CLI only | COVERED |
| Not wired into `dotnet test` CI | curated `weapon-slice-50` + `--max-records` | COVERED |
| WriteGate regression PASS | Data 197/197; Cli 11/11 | COVERED |
| ReplayGolden 6/6 unchanged | `ReplayGoldenSuiteTests` 6/6 | COVERED |
| Evidence doc | `production/qa/sprint-31-nightly-weapon-2026-06-18.md` | COVERED |
| ZERO touch `DelegationBridge` | empty diff | COVERED |

## Deliverables

- `tools/cmo-nightly-import.sh` / `tools/cmo-nightly-approve.sh` — existing `--entity weapon` path exercised at full corpus scale
- `ResolveReferenceWeaponMarkdownPath()` + scale metadata test in `CmoMarkdownWeaponImportTests`
- Evidence: `production/qa/sprint-31-nightly-weapon-2026-06-18.md`

## Pinned snapshot hash (full weapon.md nightly)

| Field | Value |
|-------|-------|
| Corpus | `docs/reference/cmano-db/weapon.md` |
| `parsedCount` | 4403 |
| Propose batches | 9 (chunk 500/batch: 8×500 + 1×403) |
| Unique approve batches | 2 (`batch-weapon-500-0`, `batch-weapon-403-0`) |
| `releaseVersion` | `nightly-weapon-s31-10-20260618` |
| `snapshotId` | `baltic_patrol` |
| `contentHashSha256` (batch-weapon-500-0) | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
| `contentHashSha256` (batch-weapon-403-0) | `ad3c8e2006df679e7eac4310be1c1b6a9773b6e8033bcd60d8757a58c46c8912` |
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
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh --entity weapon --chunk-size 500 --propose-only --dry-run
./tools/cmo-nightly-approve.sh --entity weapon --dry-run
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Test-criterion traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| Curated propose→approve + hash pin | `CmoMarkdownWeaponImportTests` (S26-02 carryover) | COVERED |
| Weapon markdown parse smoke | `ProposeWeaponsFromMarkdown_stages_slice50_and_approve_commits` | COVERED |
| Bulk chunk propose path | `ChunkWeapons_with_501_rows_produces_two_batches_at_chunk_size_500` | COVERED |
| Off-CI full corpus scale metadata | `Reference_weapon_markdown_parses_4403_records_for_off_ci_nightly_scale` | COVERED |
| Off-CI full corpus run | `production/qa/sprint-31-nightly-weapon-2026-06-18.md` | COVERED |

## Not touched (by design)

- `CatalogWriteGate.cs` (extend-only; no signature changes)
- `DelegationBridge.cs`

## Unblocks

- **S31-11** — Entity corpus nightly approve at scale
- **S31-09** — Balance drift advisory on nightly approve summary