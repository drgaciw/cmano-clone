# SWARM-A1 PE/PDA gap — Platform Editor chrome deferred (DRG-86 AC)

**Date:** 2026-08-09  
**Linear:** [DRG-86](https://linear.app/drgamtd-workspace/issue/DRG-86) · Phase B umbrella [DRG-92](https://linear.app/drgamtd-workspace/issue/DRG-92)  
**Requirement:** SWARM-21 split — Phase A schema+preset **mandatory**; PE/PDA full chrome **Phase B**

## Gap statement

Platform Editor / PDA **does not** yet round-trip `platform_swarm` columns (`is_swarm`, `max_drones`, `armor_class`, default sensor/weapon ids) through workbook export → edit → propose → approve.

Phase A (this wave) lands:

- SQLite table `platform_swarm` (migration `012_swarm_platform.sql`)
- Runtime DTOs + `ICatalogReader` read path
- Seeded generic preset `uas-swarm-generic`
- Data-side `SwarmUnitFactory` / `SwarmUnitIntegrity`

Phase B (out of DRG-86 surface for PE chrome):

- Workbook sheet columns or Platforms-sheet extensions for swarm fields
- Write-gate staging (`catalog_staging_swarm` if needed) + Approve*
- PDA validation rules for maxDrones > 0, armor class enum, orphan sensor/weapon refs

## Why deferred

Owner triage (2026-08-09) and doc 22 SWARM-21: **Phase A = schema + ≥1 abstract generic preset**; PE chrome may lag so SWARM-01 can instantiate without blocking on editor UI.

## Acceptance of this gap for DRG-86

DRG-86 AC “Platform Editor / PDA can round-trip new fields **or documented gap filed**” is satisfied by **this document**. Tracking continues under DRG-92 / future PE story — do not block Wave 2 merge on PE UI.

## Reopen criteria

Close this gap when:

1. Export includes swarm columns for swarm platforms  
2. Import propose/approve round-trips without data loss  
3. Validator rejects `max_drones <= 0` and unknown armor class  
4. Evidence under `production/qa/` with PE screenshot or headless export hash  
