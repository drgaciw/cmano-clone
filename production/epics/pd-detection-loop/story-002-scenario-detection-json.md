# Story 002: Scenario detection trials JSON

> **Epic**: Pd Detection Loop  
> **Status**: Complete (PR 1)  
> **Type**: Config/Data  
> **TR**: TR-sensor-002

## Acceptance Criteria

- [x] `detection[]` in `*.policy.json` → `ScenarioDetectionTrial` on profile.
- [x] Loader test for `baltic-patrol` includes trials when present.

## Test Evidence

- `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs`