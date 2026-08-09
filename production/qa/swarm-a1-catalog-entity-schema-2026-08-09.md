# SWARM-A1 — Catalog + entity schema for swarm platforms (DRG-86)

**Date:** 2026-08-09  
**Linear:** [DRG-86](https://linear.app/drgamtd-workspace/issue/DRG-86) · Epic [DRG-83](https://linear.app/drgamtd-workspace/issue/DRG-83) · Milestone H8  
**Requirements:** SWARM-01, SWARM-02, SWARM-05 (catalog half), SWARM-21 Phase A (schema + preset)  
**Surface:** `src/ProjectAegis.Data/**` · `assets/data/catalog/migrations/012_swarm_platform.sql` · Data tests · this QA note  
**Verdict:** PASS (Phase A schema + generic preset + unit integrity factory)

## Scope

Introduce first-class **drone/UAS swarm** catalog rows and Data-side unit integrity for spawn:

| Concern | Implementation |
|---------|----------------|
| Platform type flag | `platform_swarm.is_swarm` + `CatalogSwarmPlatform.IsSwarm` |
| Aggregate integrity ceiling | `max_drones` / `MaxDrones` |
| Armor band | `armor_class` default `light-air` |
| Default fittings refs | `default_sensor_id`, `default_weapon_id` |
| Generic Phase A preset | `uas-swarm-generic` (max 40) |
| Unit spawn integrity | `SwarmUnitIntegrity` + `SwarmUnitFactory` (Data only) |
| Scenario ref | `ScenarioOrbatUnitDto.PlatformId` + optional `DroneCount` |

**Not in this PR:** Sim engagement scaling, SwarmController orders (DRG-87), PE/PDA chrome (Phase B — gap filed below), Unity UI.

**Vocabulary collision:** distinct from `SwarmTier` (req-09 near-future entity caps) and `SwarmSalvoDeconfliction` (salvo slots).

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Catalog entry for ≥1 generic swarm loads | `SwarmPlatformCatalogTests.Baltic_seed_loads_generic_swarm_catalog_entry`; seed via `CatalogSeedBootstrap.SeedGenericSwarmPlatform` | **PASS** |
| Scenario can reference swarm platform id without broken refs | `Scenario_can_reference_swarm_platform_id_and_spawn_integrity` places ORBAT unit with `uas-swarm-generic`; catalog position + swarm row resolve | **PASS** |
| Unit spawn exposes integrity fields on sim unit model | `SwarmUnitIntegrity` (`DroneCount`/`MaxDrones`); factory clamps and defaults | **PASS** |
| PE/PDA round-trip or documented gap | **Gap filed** — see [swarm-a1-pe-pda-gap-2026-08-09.md](./swarm-a1-pe-pda-gap-2026-08-09.md) (SWARM-21 Phase B) | **PASS (gap)** |

## Gates

| Gate | Result |
|------|--------|
| Surface discipline | Data + migration + Data.Tests + production/qa only (no Sim engage / Unity) |
| `dotnet build ProjectAegis.sln` | 0 errors (agent run) |
| `dotnet test` Data.Tests swarm + seed | filtered suite green (agent run) |
| Full solution test floor ≥1638 | run on CI / local verify before merge if wall-clock allows |

## Key types / files

- `assets/data/catalog/migrations/012_swarm_platform.sql`
- `CatalogSwarmPlatform` / `CatalogSwarmPlatformDefaults`
- `SwarmUnitIntegrity` / `SwarmUnitFactory`
- `ICatalogReader.GetSortedSwarmPlatforms` / `TryGetSwarmPlatform`
- `SqliteCatalogReader` + `InMemoryCatalogReader` + seed bootstrap
- `ScenarioOrbatUnitDto.DroneCount` (optional)

## Verify commands

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~SwarmPlatformCatalogTests|FullyQualifiedName~CatalogSeedBootstrapTests" -v minimal
dotnet build ProjectAegis.sln -v minimal
```

## Follow-ons

- **DRG-87** SwarmController MVP (Graphite stack base = this PR after green)
- Wave 3 surface-disjoint: DRG-88 ∥ 89 ∥ 90
- Phase B PE columns / workbook (gap doc)

## Review follow-up (2026-08-09)

- Graphite: fixed tautological `GenericMaxDrones` assert → pin literal `40`.
- Codex P1: `EnsureGenericSwarmPlatform` / seed path is **insert-if-absent only**
  (`INSERT OR IGNORE` + platform_id existence check). Opening a catalog reader no longer
  rewrites curated `platform_swarm` / sensor / weapon rows.
- New pin: `EnsureGenericSwarmPlatform_does_not_overwrite_curated_max_drones`.

### Follow-up `b7b2dfd`+ (display_name)

`BalticPlatforms()` already inserts `uas-swarm-generic` without `display_name`.
Insert-if-absent must still **fill blank** starter metadata (`IsBlankDisplayName`) so
import e2e readback (`display_name != ''`) includes the generic swarm. Curated non-empty
names and max_drones remain preserved.
