# Enterprise public corpus catalog

Load the full offline [cmano-db.com](https://cmano-db.com) markdown corpus into a **dedicated production catalog** — separate from the Baltic hybrid QA catalog.

## Targets

| Artifact | Path | Purpose |
|----------|------|---------|
| Enterprise catalog | `assets/data/catalog/aegis_public_corpus.db` | ~32k public entities via write gate |
| Baltic QA catalog | `assets/data/catalog/baltic_patrol.db` | ReplayGolden 6/6, gauntlet oracle (unchanged) |
| Snapshot id | `aegis_public_corpus` | `dbRef` / `--snapshot-id` for enterprise scenarios |
| Corpus source | `docs/reference/cmano-db/*.md` | DB version noted in `cmano-db-data.md` |

**Policy:** no proprietary CMO `*.db3`. No `--map-baltic-platform-ids` on full corpus.

## Prerequisites

- .NET 8 SDK
- Corpus files present under `docs/reference/cmano-db/` (refresh via `tools/cmano-db-crawler/harvest.mjs` + `render.mjs` when cmano-db version moves)
- `ground-units.md` committed (derived from `facility.md`; not emitted by `render.mjs`)

## Load sequence (off-CI)

### Windows (PowerShell)

```powershell
cd D:\MyCode\cmano-clone
$env:AEGIS_PUBLIC_CORPUS = '1'
.\tools\cmo-enterprise-corpus-load.ps1 -RunDate 20260722
.\tools\cmo-promote-corpus-catalog.ps1 -RunDate 20260722
```

### Git Bash / Linux

```bash
cd cmano-clone
./tools/cmo-nightly-import.sh --enterprise-corpus --entity all --propose-only
./tools/cmo-nightly-approve.sh --run-date $(date -u +%Y%m%d) \
  --snapshot-id aegis_public_corpus \
  --release-version corpus-full-$(date -u +%Y%m%d)
./tools/cmo-promote-corpus-catalog.sh $(date -u +%Y%m%d)
```

### Entity order

`sensor → weapon → platform (ship.md) → aircraft → submarine → facility → ground-unit`

Platform-family imports pass `--weapon docs/reference/cmano-db/weapon.md` so mount/magazine resolution avoids mass `orphan_weapon_id` quarantine.

### Bootstrap

`AEGIS_PUBLIC_CORPUS=1` creates a **schema-only** scratch DB (no Baltic `u1`/`hostile-1` seed). Baltic scenarios continue to use `baltic_patrol.db`.

## Verification

```powershell
.\tools\cmo-verify-corpus-coverage.ps1 -DbPath assets\data\catalog\aegis_public_corpus.db -MinCoveragePercent 99
.\scripts\verify-catalog-import.ps1
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_kill_chain_report --db assets/data/catalog/aegis_public_corpus.db
```

Coverage gate compares corpus H3 counts vs live rows (≥99% per entity).

Metric semantics (not naive `domain=` tallies): ship = surface minus enterprise
placeholders and water-(surface) facilities; submarine / ground-unit = collision-
disambiguated markdown ID overlap; facility = land + water-(surface) facility
classes. Domain-raw vs markdown-overlap diagnostics print when they diverge; the
gate always uses the hardened metric. Live `platform` has no `citation_ref` column
(provenance is staged at propose time).

## Scenario binding

Set scenario metadata:

```json
"dbRef": "aegis_public_corpus"
```

Resolved by `CatalogValidationDefaults.TryResolvePublicCorpusDbRef` and `CatalogReaderFactory.ResolvePublicCorpusDatabasePath()` → `assets/data/catalog/aegis_public_corpus.db`. Alias `public-corpus` also maps here. Baltic remains the default when `dbRef` is omitted or resolves to `baltic_patrol`.

### Smoke scenario

| Artifact | Path |
|----------|------|
| Smoke package | `assets/data/scenarios/smoke/enterprise-public-corpus-smoke.scenario.json` |

Minimal authoring doc with `"dbRef": "aegis_public_corpus"` and a 2-unit OOB using live public-corpus `platform_id`s (`type-082ii-wozang-818-kunshan-2012`, `type-203-huxin-haijing-44072-1988`). Intended for **dbRef / factory path smoke only** — not ReplayGolden, not gauntlet.

**Load + bind check (unit test):**

```powershell
dotnet test src/ProjectAegis.Data.Tests --filter FullyQualifiedName~CatalogPublicCorpusTests.Smoke_scenario_file_resolves_public_corpus_dbRef_to_enterprise_path
```

**Manual inspect:**

```powershell
# Confirm metadata.dbRef and OOB platformIds
Get-Content assets\data\scenarios\smoke\enterprise-public-corpus-smoke.scenario.json
# Factory target (must exist after promote / LFS pull)
Test-Path assets\data\catalog\aegis_public_corpus.db
```

## Do not use

| Tool / flag | Why |
|-------------|-----|
| `import-qa-slice` on full corpus | Auto-approves; fixture scope only |
| `--map-baltic-platform-ids` | Collapses unrelated hulls onto scenario ids |
| Promote to `baltic_patrol.db` | Breaks replay / gauntlet contracts |
| `MAX_RECORDS` caps (enterprise run) | Smoke/CI only |

## Artifact storage

| Option | When | Notes |
|--------|------|-------|
| **Git LFS** (preferred) | `git lfs` available; `.gitattributes` tracks `assets/data/catalog/aegis_public_corpus.db` | Promoted DB is ~65 MB. Commit via LFS after promote. |
| **Release / external artifact** | LFS unavailable or policy forbids large binaries in-repo | Attach `aegis_public_corpus.db` to a GitHub Release (or equivalent) and document the download URL for local checkout. |

**Never** replace or overwrite `assets/data/catalog/baltic_patrol.db` — it remains the Baltic hybrid QA catalog (ReplayGolden / gauntlet). Promote scripts target only `aegis_public_corpus.db`.

## See also

- [dual-track-cmo-analysis-and-catalog.md](dual-track-cmo-analysis-and-catalog.md)
- [cmo-markdown-import.md](cmo-markdown-import.md)
- [aegis-catalog-sdlc-orchestration.md](aegis-catalog-sdlc-orchestration.md)
