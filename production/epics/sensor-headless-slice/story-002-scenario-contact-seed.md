# Story 002: Scenario contact seeds + deterministic simulator

> **Epic**: Sensor Headless Slice
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 4h

## Context

**GDD**: `design/gdd/sensor-detection-ew.md` — detection tick sorted iteration
**Requirement**: TR-sensor-002 (MVP schedule, not Pd roll)

**Governing ADRs**: ADR-004 (tick ordering)

## Acceptance Criteria

- [x] `contacts[]` in `data/scenarios/*.policy.json` → `ScenarioContactSeed`.
- [x] `ScenarioContactSimulator` sorted `(observer, target, contactId)`; Unknown→Detected at `appearAtTick`.
- [x] Two runs → identical transition list (property test).
- [x] `baltic-patrol` seeds primary hostile at tick 1.

## Test Evidence

- `src/ProjectAegis.Sim.Tests/Sensors/ScenarioContactSimulatorTests.cs`
- `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs`