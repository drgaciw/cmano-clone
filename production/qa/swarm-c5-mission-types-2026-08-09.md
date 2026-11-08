# SWARM-C5 / DRG-109 — Mission types for swarm tasking

**Date:** 2026-08-09  
**Surface:** `src/ProjectAegis.Data/Scenario/Authoring/**`, Data.Tests/Scenario  
**Requirement:** SWARM-20

## ACs

| AC | Evidence |
|----|----------|
| Mission type enum/constants | `SwarmMissionType` + `SwarmMissionTypeNames` |
| Default mode mapping | `SwarmMissionDefaults.DefaultMode` — Patrol→Hold, Support→Screen, Strike→Assault |
| Authoring field | `ScenarioOrbatUnitDto.MissionType` (+ resolved `Mode`) |
| JSON round-trip | `Round_trip_json_preserves_MissionType_and_Mode` |
| Unknown mission rejected | `Unknown_mission_type_rejected` / pure validation |
| Default mode when mode omitted | `Place_patrol_without_mode_applies_hold` (and Support/Strike) |
| Explicit mode wins | `Explicit_mode_overrides_mission_default` |

## Types

- `SwarmMissionType` { Patrol, Support, Strike }
- `SwarmMissionDefaults.DefaultMode`
- `ScenarioOrbatUnitDto.MissionType` / `Mode`
- `SwarmScenarioValidation.ResolveMissionAssignment`
- `ScenarioDocumentEditor.PlaceSwarmUnit` / `ConfigureSwarmUnit` mission params

## Tests

`SwarmMissionTypeTests` + existing `SwarmScenarioPlacementTests`.
