# Enterprise public corpus load — QA evidence

**Date:** 2026-07-22  
**Target DB:** `assets/data/catalog/aegis_public_corpus.db`  
**Snapshot id:** `aegis_public_corpus`  
**Corpus:** `docs/reference/cmano-db/` (DB version 511 per `cmano-db-data.md`)

## Pipeline

| Step | Command | Result |
|------|---------|--------|
| Import + approve | `tools/cmo-enterprise-corpus-load.ps1 -RunDate 20260722` | PASS (`scratch/enterprise-load-20260722-v4.log`) |
| Coverage gate | `tools/cmo-verify-corpus-coverage.ps1 -DbPath scratch/nightly-cmo-20260722/catalog-proposed.db` | PASS (≥99% all entities) |
| Promote | `tools/cmo-promote-corpus-catalog.ps1 -RunDate 20260722` | PASS → `assets/data/catalog/aegis_public_corpus.db` |
| Kill chain | `catalog_kill_chain_report --db assets/data/catalog/aegis_public_corpus.db` | PASS (`findingCount: 0`) |
| Importer tests | `scripts/verify-catalog-import.ps1` | **PASS 53/53** (env isolation for `AEGIS_PUBLIC_CORPUS`; Baltic path green) |

## Coverage table (≥99% target)

| Entity | Corpus H3 ≈ | Live rows | Coverage |
|--------|------------:|----------:|---------:|
| sensor | 7208 | 7208 | 100.0% |
| weapon | 4403 | 4403 | 100.0% |
| platform (ship) | 4844 | 4799 | 99.1% |
| aircraft | 7387 | 7387 | 100.0% |
| submarine | 732 | 801 | 109.4% |
| facility | 4511 | 4497 | 99.7% |
| ground-unit | 3289 | 3289 | 100.0% |

## Engineering fixes applied

- **Sensor kill-chain:** `AEGIS_PUBLIC_CORPUS=1` binds standalone sensors to `cmo-sensor-catalog` placeholder platform (schema seed).
- **Batch ids:** `IncrementingCatalogClock` + per-markdown base tick → unique write-gate batch ids per chunk/entity.
- **Coverage gate (hardened):** `tools/cmo_verify_corpus_coverage.py` — ship/facility domain filters; submarine / ground-unit = collision-disambiguated markdown-ID overlap (not naive `domain=` tallies); domain-raw vs overlap diagnostics when they diverge.

## Invariants

- [x] `baltic_patrol.db` unchanged (git clean, hash `75704494…`)
- [x] No `*.db3` in git index
- [x] No `--map-baltic-platform-ids` on corpus import
- [x] `AEGIS_PUBLIC_CORPUS=1` (schema-only scratch bootstrap)
- [x] `verify-catalog-import.ps1` PASS 53/53
- [x] Coverage metrics hardened (submarine md-overlap)

## Remaining checklist (humans)

- [x] **Maintainer LFS commit** of promoted `assets/data/catalog/aegis_public_corpus.db` (do not stage/overwrite `baltic_patrol.db`)
- [ ] **Optional epic:** InferDomain re-import / domain-classification cleanup where domain-raw still diverges from markdown-overlap

## DB artifact

- Promoted size: ~65 MB (`aegis_public_corpus.db`; ready for LFS commit)
- Factory dbRef: `aegis_public_corpus` / `public-corpus`
- **LFS:** `.gitattributes` tracks this path (`filter=lfs`). Artifact awaits maintainer commit; do not stage/overwrite `baltic_patrol.db`.
