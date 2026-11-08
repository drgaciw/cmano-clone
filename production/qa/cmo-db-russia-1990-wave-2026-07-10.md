# cmo-db Russia 1990+ focused catalog wave

**Date:** 2026-07-10  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Fixtures:** `tools/cmano-db-crawler/fixtures/baltic-russia-1990-{platforms,weapons,sensors}.md`  
**Import path:** `ImportQaSlice` → `CmoMarkdownImportProposer` + `CatalogWriteGate`  
**Source:** https://cmo-db.com/en/cmo/ search + detail (Russia [1992-] nationality; commission ≥ 1990)

## Scope

Focused **multi-domain** Red ORBAT depth for Baltic/theater QA:

- **Surface:** modern frigates/corvettes/destroyers (Gorshkov, Grigorovich, Karakurt, Udaloy MLU, Sovremenny, Bykov)
- **Air:** MiG-31, Su-57, Su-33, Ka-27M, Tu-160
- **Sub:** Yasen / Yasen-M, Borei / Borei-A, Akula I Improved

Not a wholesale dump of every Russian class in cmo-db.

## Baseline → after (Russia nationality)

| Domain | Baseline Russia | After Russia | Δ |
|--------|----------------:|-------------:|--:|
| Surface | 4 | **10** | **+6** |
| Air | 6 | **11** | **+5** |
| Subsurface | 2 | **7** | **+5** |
| **Russia total** | **12** | **28** | **+16** |

| Catalog total platforms | 55 → **71** |
| Weapons max_range > 0 | 208 → **260** (0 zero-range) |

## New platform_ids (all combat-usable: sensor + ranged magazine)

### Surface
- `skr-admiral-sergey-gorshkov-pr-2235-0` (2021)
- `skr-admiral-grigorovich-pr-1135-6m` (2018)
- `mrk-shkval-pr-22800-karakurt` (2019)
- `bpk-marshal-shaposhnikov-udaloy-i-pr-1155-fregat` (2020 MLU)
- `em-sovremenny-i-pr-956-sarych` (2000)
- `pk-368-vasily-bykov-pr-22160` (2023)

### Air
- `mig-31-foxhound` (1992)
- `su-57-felon` (2020)
- `su-33-flanker-d` (2016)
- `ka-27m-helix-a` (2017)
- `tu-160-blackjack` (2011)

### Subsurface
- `pla-885-severodvinsk-yasen` (2014)
- `pla-885m-severodvinsk-yasen-m` (2024)
- `plarb-955-borei-borey` (2012)
- `plarb-955a-borei-ii-borey-a` (2020)
- `pla-971-akula-i-improved-shchuka-b` (2005)

## Quality notes

- Write-gate: 16 platforms, 64 weapons, 48 magazines, 93 sensors approved; **0** fitting quarantine / **0** failed batches.
- `InferDomain` fixed so **Anti-Submarine** aircraft/helicopters map to **air** (not subsurface); Ka-27 corrected in DB.
- Combat weapons include Kalibr, Bulava, R-37M, Kh-101, UGST Fizik, etc. (`cmo-weapon-81001`+ synthetic numeric IDs where needed for importer).

## Legal

Internal design reference via community viewer cmo-db.com; site TDM rights reserved. Do not ship proprietary CMO product DB wholesale.
