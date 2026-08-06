# Scenario Editor Platform Combo UAT — 2026-07-20

## Goal
Test ability to call the **platform catalog database**, place units, and **change platforms** across **100 platform combinations** and **10 scenario variations**.

## Sources
| Source | Role |
|--------|------|
| `ScenarioPlatformDomainCatalog` | UI picker presets (20 platforms, 4 domains) |
| `assets/data/catalog/baltic_patrol.db` | SQLite catalog (**79** `platform` rows) |
| `ScenarioEditCommandBus.PlaceUnit` | Map place (insert-only) |
| `ScenarioEditCommandBus.UpsertUnit` | Platform **change** (replace by unit id) — **added this UAT** |

## Matrix
- **10 variations:** empty-place, seeded-add, dup-id-reject, platform-change-upsert, opposing-pair-place, multi-domain-pair, save-reload-roundtrip, catalog-db-place, stale-gesture-then-place, place-then-change-platform
- **100 combos:** blue×red platform pairs from domain catalog **augmented with SQLite platform ids**
- **Dense place:** 100 opposing blue/red pair places into empty scenarios

## Results
| Test | Result |
|------|--------|
| `Domain_catalog_and_sqlite_db_expose_platforms_for_authoring` | PASS (domain ≥16, SQLite ≥50) |
| `Builds_100_distinct_platform_combinations` | PASS |
| `Ten_scenario_variations_are_named_and_stable` | PASS |
| `Uat_100_platform_combos_across_10_scenario_variations` | PASS (100 runs: 10×10) |
| `Uat_100_combos_place_opposing_pairs_into_empty_scenarios` | PASS (100 pair places) |

## Code changes (TDD)
1. **`ScenarioEditCommandBus.UpsertUnit`** — explicit platform-change path (same unit id, new platform)
2. **`ScenarioPlatformComboUatTests`** — combinatorial UAT harness reading SQLite + domain catalog

## How to re-run
```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests -c Release \
  --filter "FullyQualifiedName~ScenarioPlatformComboUatTests"
```

## Note
UI list still uses domain catalog (20 presets). DB platforms are exercised via place/upsert API (same path as form free-text Platform field after catalog fill). Full SQLite-backed UI list remains a follow-on product feature.
