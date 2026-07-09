# cmo-db.com multi-domain catalog import evidence

**Date:** 2026-07-09  
**Method:** Playwright Chromium headless browser automation (`scrape-platforms.mjs`)  
**Source:** https://cmano-db.com/ (platform/sensor/weapon detail pages)  
**Legal:** Internal design reference / staged curated import only. Site reserves TDM rights (EU Dir. 2019/790 Art. 4). Not redistributing CMO proprietary product DB.

## Baseline → Post-import

| Table | Before | After |
|---|---|---|
| platform | 3 | 19 |
| sensor | 2 | 106 |
| weapon_catalog | 2 | 19 |
| platform_mount | 2 | 85 |
| platform_loadout | 1 | 17 |
| platform_magazine | 2 | 22 |

**Domains:** surface=9, air=6, subsurface=4

## Harvest
- Targets: 16 combat platforms (Baltic-relevant ships/air/subs: Visby, Orkan, Steregushchiy, Sachsen, Slazak, Buyan-M, Gripen, Su-27/30/35, Gotland, Type 212A, Kilo, F-16)
- Related: 91 sensor pages + 17 weapon pages scraped
- Artifacts: `/tmp/grok-goal-539a46171f2e/implementer/cmo-db-scrape/` (`harvest.json`, `pages/*.html`, `scrape.log`)

## Import path
- Fixtures: `tools/cmano-db-crawler/fixtures/baltic-multidomain-*.md`
- Tool: `tools/cmano-db-crawler/import-qa-slice` → `CmoMarkdownImportProposer` + `CatalogWriteGate` propose→approve
- Log: `catalog-import.log` (7 batches committed, 0 failed; 87 magazine fittings quarantined as orphan_weapon_id — expected when weapon detail not in the 17-weapon slice)

## Scenario
- Policy: `data/scenarios/baltic-multidomain-visby-sample.policy.json`
- Run: seed 42, 6 ticks — exit 0; fingerprint shows contact `k-31-visby-2009` → `mpk-steregushchiy-pr-20380-2018`

## Tests
- Data.Tests: 627 passed (incl. `BalticMultidomainImportResolutionTests` ×2)
