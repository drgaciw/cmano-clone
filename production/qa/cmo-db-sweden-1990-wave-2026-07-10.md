# cmo-db Sweden 1990+ wave — Baltic counterpart to Russian Baltic Fleet

**Date:** 2026-07-10  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Fixtures:** `tools/cmano-db-crawler/fixtures/baltic-sweden-1990-{platforms,weapons,sensors}.md`  
**Import:** `ImportQaSlice` → write-gate (`CmoMarkdownImportProposer` + `CatalogWriteGate`)  
**Source:** https://cmo-db.com/en/cmo/ (Sweden nationality)

## Scope (report-aligned)

Swedish **3rd Naval Warfare Flotilla lineage** + **1st Submarine Flotilla** + joint air for Baltic scenarios vs Soviet/Russian Baltic Fleet (1990–2026 checkpoints). Not a full dump of every Swedish hull in cmo-db.

| Checkpoint era | Report formations | Wave coverage |
|---|---|---|
| 1990 | 4th Surface Attack Flotilla, missile boats, early sub force | Hugin FAC, Göteborg lineage, Södermanland (Västergötland mod) |
| 2000 | 3rd Surface Warfare Flotilla, Gotland-class boom | Göteborg 2000s, Carlskrona support, Super Puma SAR |
| 2010 | 3rd Naval Warfare Flotilla, Visby entering | Koster MCM (already had Visby K31/Gotland/Gripen pre-wave) |
| 2020–2026 | Full Visby set, Gävle modernization, NH90 | **Gävle 2022 MLU**, HKp 14E/F, Argus AEW, A26 Blekinge (future SS) |

Pre-existing Sweden (not re-added): Visby K31, Stockholm K11 Spica III, Gotland A19 year set, Västergötland A17, JAS 39A–E.

## Baseline → after (Sweden nationality)

| Domain | Baseline | After | Δ |
|--------|---------:|------:|--:|
| Surface | 2 | **7** | **+5** |
| Air | 5 | **10** | **+5** |
| Subsurface | 5 | **7** | **+2** |
| **Sweden total** | **12** | **24** | **+12** |

| Catalog total platforms | 71 → **83** |
| Weapons max_range > 0 | 260 → **280** (0 zero-range) |

## New platform_ids (all combat-usable: sensor + ranged magazine)

### Surface (3rd NWF lineage / MCM / support / FAC)
- `k-22-gavle-ex-goteborg-class` (2022) — Gävle class modernized
- `k-21-goteborg` (2007) — Göteborg class
- `m-73-koster-upgraded-landsort-class` (2009) — Koster MCMV
- `m-04-carlskrona` (2002) — command/support
- `p-151-hugin` (1985) — 1990-era missile FAC concept still listed

### Subsurface (1st Submarine Flotilla)
- `a-17-södermanland-vastergotland-mod` (2004) — Södermanland (Västergötland mod)
- `a-26-blekinge` (2031) — A26 next-gen SS

### Air (joint Baltic)
- `hkp-14f-nh90-ttt` (2020) — NH90 ASW
- `hkp-14e-nh90-ttt` (2010) — NH90 TTT
- `as-332m1-super-puma-hkp-10b` (2000) — SAR helo
- `sk-60a` (1999) — trainer/light attack
- `saab-340-aew-s-100b-argus` (2000) — Erieye AEW

## Quality

- Write-gate: 12 platforms, 20 weapons, 24 magazines, 57 sensors; **0** quarantine / **0** failed batches.
- NH90 ASW remains **air** (InferDomain anti-submarine-before-submarine order).
- Combat weapons: RBS 15 family, Tp 62/47, Rb 99/98, Erieye sensors, etc. (`cmo-weapon-82001`+).

## Legal

Internal design reference via cmo-db.com community viewer; site TDM rights reserved. Do not ship proprietary CMO product DB wholesale.
