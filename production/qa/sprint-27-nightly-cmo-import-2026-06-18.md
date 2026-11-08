# Sprint 27 — Nightly CMO Corpus Import Evidence (S27-02)

**Date:** 2026-06-18  
**Story:** S27-02 — Nightly full CMO corpus import job  
**Script:** `tools/cmo-nightly-import.sh`

## Verdict: **PASS** (local gate with `MAX_RECORDS=12`; full 7208 run is off-CI)

| AC | Evidence | Result |
|----|----------|--------|
| Job script invokes `catalog_import_markdown` per entity | `tools/cmo-nightly-import.sh` | **PASS** |
| Sensor + weapon corpora in v1 | `docs/reference/cmano-db/sensor.md`, `weapon.md` | **PASS** |
| Chunk 500/batch | `CHUNK_SIZE=500` default | **PASS** |
| Quarantine JSON via `--report-out` | `scratch/nightly-cmo-20260618/*-quarantine.json` | **PASS** |
| Propose-only (no auto-approve) | Staged batches only in scratch DB | **PASS** |
| Not in `dotnet test` CI | Script is standalone under `tools/` | **PASS** |
| Platform slices excluded v1 | Script imports sensor + weapon only | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
MAX_RECORDS=12 tools/cmo-nightly-import.sh
```

**Sensor propose JSON:** `parsedCount=12`, `batchCount=1`, `quarantinedCount=0`  
**Artifacts:** `scratch/nightly-cmo-20260618/sensor-propose.json`, `weapon-propose.json`, quarantine reports

## Production nightly

Unset `MAX_RECORDS` for full `sensor.md` (7208) + `weapon.md` propose-only runs. Approve batches manually via `catalog_write_approve` before any catalog commit.