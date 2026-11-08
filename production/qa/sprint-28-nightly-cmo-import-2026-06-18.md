# Sprint 28 — Nightly CMO Corpus v2 Platform Slices Evidence (S28-02)

**Date:** 2026-06-18  
**Story:** S28-02 — Nightly CMO corpus v2 — platform slices  
**Script:** `tools/cmo-nightly-import.sh`

## Verdict: **PASS** (local gate with `MAX_RECORDS=12`; full ship.md run is off-CI)

| AC | Evidence | Result |
|----|----------|--------|
| Script extended for platform entity slices (v2) | `tools/cmo-nightly-import.sh` — `--entity platform` + `ship.md` | **PASS** |
| Sensor + weapon v1 behavior intact | Default `--entity all` still runs sensor + weapon | **PASS** |
| Chunk 500/batch | `CHUNK_SIZE=500` default; `--chunk-size` flag | **PASS** |
| Quarantine JSON via `--report-out` | `scratch/nightly-cmo-20260618/*-quarantine.json` | **PASS** |
| Propose-only (no auto-approve) | `--propose-only` default; staged batches only in scratch DB | **PASS** |
| Curated fixture path documented | `tools/cmano-db-crawler/fixtures/ship-slice-100.md` via `PLATFORM_MD` override | **PASS** |
| Not in `dotnet test` CI | Script is standalone under `tools/` | **PASS** |
| Full 7208-record sensor stays off-CI | CI uses curated fixtures + `--max-records` only | **PASS** |
| ZERO touch `DelegationBridge.cs` | No changes under `Delegation.UnityAdapter/Bridge/` | **PASS** |

## Local gate run

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|Platform|CatalogImport" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "CatalogImport|Platform" -v minimal
MAX_RECORDS=12 ./tools/cmo-nightly-import.sh
```

**Build:** 0 errors  
**Data.Tests:** 143/143 PASS  
**Cli.Tests:** 7/7 PASS

### All-entity smoke (`MAX_RECORDS=12`)

| Entity | parsedCount | batchCount | quarantinedCount |
|--------|-------------|------------|------------------|
| sensor | 12 | 1 | 0 |
| weapon | 12 | 1 | 0 |
| platform (`ship.md`) | 12 | 3 | 25239 *(orphan fittings from partial slice — expected)* |

**Artifacts:** `scratch/nightly-cmo-20260618/sensor-propose.json`, `weapon-propose.json`, `platform-propose.json`, quarantine reports

### Platform-only curated gate (`ship-slice-100.md`)

```bash
PLATFORM_MD=tools/cmano-db-crawler/fixtures/ship-slice-100.md \
  ./tools/cmo-nightly-import.sh --entity platform --chunk-size 500 --propose-only --max-records 12
```

**Platform propose JSON:** `parsedCount=12`, `batchCount=1`, `quarantinedCount=0`

### Dry-run validation

```bash
./tools/cmo-nightly-import.sh --dry-run --entity platform --max-records 12
```

Paths validated; no dotnet invocations.

## Production nightly

Unset `MAX_RECORDS` for full corpora:

- `docs/reference/cmano-db/sensor.md` (7208)
- `docs/reference/cmano-db/weapon.md`
- `docs/reference/cmano-db/ship.md` (4844 platforms)

Propose-only runs stage batches in `scratch/nightly-cmo-YYYYMMDD/catalog-proposed.db`. Approve manually via `catalog_write_approve` before any catalog commit.

## Partial failure recovery

- Each entity import is independent; a sensor/weapon failure does not block platform if run with `--entity platform`.
- Re-run the script on the same date to refresh scratch artifacts under `scratch/nightly-cmo-YYYYMMDD/`.
- Quarantine JSON per entity supports triage without committing rejected rows.