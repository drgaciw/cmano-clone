# cmo-db Sweden 1990+ wave — Baltic counterpart to Russian Baltic Fleet

**Date:** 2026-07-10  
**Catalog:** `assets/data/catalog/baltic_patrol.db`  
**Fixtures:** `tools/cmano-db-crawler/fixtures/baltic-sweden-1990-{platforms,weapons,sensors}.md`  
**Import:** `ImportQaSlice` → write-gate (`CmoMarkdownImportProposer` + `CatalogWriteGate`)  
**Source:** https://cmo-db.com/en/cmo/ (Sweden nationality; mount tables → munitions)

## Scope (report-aligned)

Swedish **3rd Naval Warfare Flotilla lineage** + **1st Submarine Flotilla** + joint combat air for Baltic scenarios vs Soviet/Russian Baltic Fleet (1990–2026 checkpoints). Not a full dump of every Swedish hull in cmo-db.

| Checkpoint era | Report formations | Wave coverage |
|---|---|---|
| 1990 | 4th Surface Attack Flotilla, missile boats, early sub force | Hugin FAC (Penguin), Göteborg lineage, Södermanland (Västergötland mod) |
| 2000 | 3rd Surface Warfare Flotilla, Gotland-class boom | Göteborg 2000s, Carlskrona support (guns only; no invented SAM) |
| 2010 | 3rd Naval Warfare Flotilla, Visby entering | Koster MCM (pre-wave Visby K31 / Gotland / Gripen retained) |
| 2020–2026 | Full Visby set, Gävle modernization, NH90 ASW | **Gävle 2022 MLU** (RB 15M Mk2), HKp 14F (Tp 45), A26 Blekinge |

Pre-existing Sweden (not re-added): Visby K31, Stockholm K11 Spica III, Gotland A19 year set, Västergötland A17, JAS 39A–E.

## Baseline → after (Sweden nationality)

| Domain | Baseline | After (honest remediation) | Δ vs baseline |
|--------|---------:|---------------------------:|--------------:|
| Surface | 2 | **7** | **+5** |
| Air | 5 | **6** | **+1** (combat NH90 ASW only; chrome dropped) |
| Subsurface | 5 | **7** | **+2** |
| **Sweden total** | **12** | **20** | **+8** |

| Catalog total platforms | 71 → **79** |
| Weapons with max_range > 0 | 260 → **265** (0 zero-range) |

### Chrome excluded from combat counts (removed from catalog)

Not combat-usable under wave rules (no positive-range offensive magazines from cmo-db mounts/loadouts):

- `hkp-14e-nh90-ttt` — cargo/trooper only  
- `as-332m1-super-puma-hkp-10b` — cargo/trooper only  
- `sk-60a` — no weapons on cmo-db page  
- `saab-340-aew-s-100b-argus` — AEW sensors only (no armament)

## New combat platform_ids (sensor + magazine→weapon max_range > 0)

### Surface (3rd NWF lineage / MCM / support / FAC)

| platform_id | Honest munitions (cmo-db weapon IDs) |
|---|---|
| `k-22-gavle-ex-goteborg-class` | RB 15M Mk2 (`1455`), 57mm Mk3 (`1140`), 40mm Mk3 (`1347`), Tp 432 (`1380`) |
| `k-21-goteborg` | RB 15M Mk2 (`1455`), 57mm Mk2 (`1495`), 40mm Mk3 (`1347`), Tp 432 (`1380`) |
| `m-73-koster-upgraded-landsort-class` | 57mm Mk1 HCER (`1437`), 7.62mm MG (`626`) |
| `m-04-carlskrona` | 57mm Mk1 HCER (`1437`), 40mm Mk3 (`1347`), 7.62mm MG (`626`) — **no RBS 70** (not on mounts) |
| `p-151-hugin` | Penguin Mk2 (`1475`), 57mm Mk1 (`1437`), Elma ASW soft (`1407`) — **not RBS 15** |

### Subsurface (1st Submarine Flotilla)

| platform_id | Honest munitions |
|---|---|
| `a-17-södermanland-vastergotland-mod` | Tp 613 (`349`) 533mm; Tp 45 (`1595`) for mount comment Tp 451 (no separate Tp 451 page) |
| `a-26-blekinge` | Tp 47 (`3936`), Tp 62 (`901`) — **no invented NLOS cruise** |

### Air (joint Baltic combat)

| platform_id | Honest munitions |
|---|---|
| `hkp-14f-nh90-ttt` | Tp 45 (`1595`) only (sonobuoys zero-range; not counted as weapons) |

## Loadout honesty remediation (2026-07-10)

Skeptic rejected invented fixtures (RBS 70, door guns, Sidewinder, wrong ASuW missiles). Remediation:

1. Re-scraped mount tables from cmo-db detail pages.  
2. Mapped mounts → real munitions only (`sweden-loadout-provenance.txt`).  
3. Cleared invent `cmo-weapon-820xx` magazines; re-imported via write-gate.  
4. Dropped chrome air rows from production catalog.

## Quality

- Write-gate remediation: 8 platforms, 13 weapons, 21 magazines, 42 sensors; **0** quarantine / **0** failed batches.  
- NH90 ASW remains **air** (InferDomain).  
- Gavle ASuW is **RB 15M Mk2**, not Penguin; Penguin is **Hugin-only**.  
- All eight combat IDs: ≥1 sensor and ≥1 magazine weapon with `max_range_meters > 0`.

## Legal

Internal design reference via cmo-db.com community viewer; site TDM rights reserved. Do not ship proprietary CMO product DB wholesale.
