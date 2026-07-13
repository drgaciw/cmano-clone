# S33-02 — Dependency Graph Index Evidence

**Story:** `production/epics/sprint-33-kill-chain-intelligence/story-033-02-dependency-graph-index.md`  
**Date:** 2026-06-19  
**Owner:** team-data  
**Branch:** `stack/sprint33/dependency-graph-index`

## Summary

Sorted kill-chain dependency edges (`platform→mount`, `platform→mount→weapon`, `platform→sensor`) are materialized in `CatalogDependencyGraphIndex` and exposed via `ICatalogReader.GetSortedDependencyEdges()`. `SqliteCatalogReader` caches edges and upstream mount/magazine/sensor slices; `CatalogWriteGate.ApproveBatch` commit paths invalidate via `CatalogDependencyGraphCacheInvalidator` (extend-only).

## API

| Symbol | Role |
|--------|------|
| `CatalogDependencyEdge` | Edge DTO with `(platformId, mountId, weaponId, sensorId)`; empty string = unused dimension |
| `CatalogDependencyEdgeKind` | `PlatformToMount`, `PlatformToMountToWeapon`, `PlatformToSensor` |
| `CatalogDependencyGraphIndex.BuildFrom` | Deterministic edge builder from mounts/magazines/sensors |
| `ICatalogReader.GetSortedDependencyEdges()` | Public read API; default empty for non-materializing readers |
| `CatalogDependencyGraphCacheInvalidator` | Notifies registered `SqliteCatalogReader` instances per DB path |

## Quarantine / approval gate

Edges include only bindings with `review_state == approved` (ordinal ignore-case) for mounts and sensors. Magazine weapon chains require an approved mount key.

## Verify commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DependencyGraph|WriteGate|Platform" -v minimal
dotnet test ProjectAegis.sln -v minimal
```

## Test counts (2026-06-19)

| Suite | Before (S32) | After (S33-02) | Delta |
|-------|--------------|----------------|-------|
| `DependencyGraph\|WriteGate\|Platform` filter | 152 | **165** | +13 |
| `ProjectAegis.Data.Tests` full | 335 | **348** | +13 |
| `ProjectAegis.sln` full | 1073 | **1092** | +19 |

### New tests (`DependencyGraphIndexTests.cs`)

1. `DependencyGraph_BuildFrom_orders_edges_by_stable_sort_key`
2. `DependencyGraph_BuildFrom_emits_platform_mount_edges`
3. `DependencyGraph_BuildFrom_emits_platform_mount_weapon_chain_edges`
4. `DependencyGraph_BuildFrom_emits_platform_sensor_edges`
5. `DependencyGraph_BuildFrom_excludes_rejected_mounts`
6. `DependencyGraph_BuildFrom_excludes_rejected_sensors`
7. `DependencyGraph_BalticMagazineFixture_exposes_weapon_chain_edges`
8. `DependencyGraph_BalticPatrolFixture_exposes_sensor_edges_only`
9. `DependencyGraph_BuildFrom_is_deterministic_across_rebuilds`
10. `DependencyGraph_NullCatalogReader_returns_empty_edges`
11. `DependencyGraph_SqliteCatalogReader_caches_edges_until_invalidated`
12. `DependencyGraph_ApproveBatch_mount_commit_invalidates_reader_cache`
13. `DependencyGraph_ApproveBatch_sensor_commit_refreshes_sensor_edges`

## Hard gates

| Gate | Result |
|------|--------|
| `CatalogWriteGate` extend-only | PASS — `NotifyDependencyGraphCommitted()` added after approve commits only |
| ZERO touch `DelegationBridge.cs` | PASS — no diff |
| Baltic + ship-slice-100 fixtures only in CI | PASS — tests use `CatalogSeedBootstrap.SeedBalticPatrol` + in-memory Baltic fixtures |
| Full sln ≥1073 | PASS — **1092/1092** |

## Files changed

- **NEW** `src/ProjectAegis.Data/Catalog/CatalogDependencyEdge.cs`
- **NEW** `src/ProjectAegis.Data/Catalog/CatalogDependencyGraphIndex.cs`
- **NEW** `src/ProjectAegis.Data/Catalog/CatalogDependencyGraphCacheInvalidator.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/ICatalogReader.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs`
- **MODIFY** `src/ProjectAegis.Data/Catalog/NullCatalogReader.cs`
- **MODIFY** `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`
- **NEW** `src/ProjectAegis.Data.Tests/Catalog/DependencyGraphIndexTests.cs`
- **NEW** `production/agentic/sprint-33-dependency-graph-2026-06-19.md`