# cmo-db.com multi-domain catalog import evidence (fixed ranges + Visby magazines)

**Date:** 2026-07-09  
**Method:** Playwright Chromium (`scrape-platforms.mjs`) + platform HTML weapon/range extraction  
**Source:** https://cmano-db.com/  
**Legal:** Internal design reference only; not redistributing CMO proprietary DB.

## Counts
| Table | Count |
|---|---|
| platform | 19 (3 seed + 16 harvested) |
| weapon_catalog | 109 (all max_range_meters > 0) |
| platform_magazine (Visby) | 2 (`cmo-weapon-455`, `cmo-weapon-1140`) |

## Weapon fixture fix
`ReadWeaponBindings` only parses ranges under `**Weapons**` with `Air/Surface/Land/Sub Max: N km`.
Fixtures rewritten from platform scrape tables (107 weapons). Zero-range count = 0.

## Visby showcase
- Scraped weapon detail pages: `weapon/455`, `weapon/1140` (HTTP 200)
- Magazines: Dual Trap + 57mm Bofors linked via name-matched platform lines

## Tests
BalticMultidomainImportResolutionTests: 4/4  
Data.Tests: see post-import-tests.log

## Scenario
`baltic-multidomain-visby-sample` seed 42 → exit 0 (see scenario-new-platform-run.log)
