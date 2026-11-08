# SWARM-B9 / DRG-101 — Scenario editor place/configure swarm

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Data/Scenario/Authoring/**`, Data.Tests/Scenario  
**Requirement:** SWARM-22

## ACs

| AC | Evidence |
|----|----------|
| Place count ≤ max | `Place_swarm_with_count_within_max_succeeds` |
| Count > max fails | `Place_with_count_over_max_fails` |
| Host assign | `Host_assign_persists_on_dto` |
| JSON round-trip | `Round_trip_json_preserves_DroneCount_and_HostUnitId` |
| Configure | `Configure_updates_count` |

## Types

- `ScenarioOrbatUnitDto.HostUnitId`
- `ScenarioDocumentEditor.PlaceSwarmUnit` / `ConfigureSwarmUnit`
- `SwarmScenarioValidation`
