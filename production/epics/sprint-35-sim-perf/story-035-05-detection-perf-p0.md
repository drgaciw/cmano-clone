---
id: S35-05
status: Complete
completed: 2026-06-19
type: Logic
priority: must-have
graphite_branch: stack/sprint35/sim-perf-p0
estimate_days: 3
dependencies:
  - S35-01 green baseline
owner: team-simulation
sprint: 35
req_trace: TR-sensor-001; ARCH-NFR-1; perf-profile P0 hotspots
governing_adrs: ADR-003 (order log sequencing — no behavior change)
sprint_gate: true
---

# Story 035-05 — Sim Perf P0 — Detection Hot Path

> **Epic:** sprint-35-sim-perf

## Summary

Eliminate P0 perf hotspots: `Dictionary<string, ScenarioDetectionTrial>` in `PdDetectionContactSimulator` (replace `First()` scans) and pre-sorted trials in `DeterministicDetectionLoop` (remove per-tick `OrderBy`). **Bit-identical** tick output required.

## Acceptance Criteria

- [x] No hot-path `_trials.First(t => t.ContactId == …)` in `PdDetectionContactSimulator`
- [x] `DeterministicDetectionLoop.RollTick` does not allocate per-tick `OrderBy().ToArray()`
- [x] Sort key preserved: ObserverId → SensorId → TargetId (deterministic)
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS
- [x] Production Baltic world hash `17144800277401907079` unchanged
- [x] `/replay-verify` PASS on merge
- [x] `dotnet test --filter "PdDetection|DeterministicDetection"` — all PASS
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

```
Test: ReplayGolden after P0 perf refactor
  Given: S35-05 branch merged to sim hot path
  When: ReplayGoldenSuiteTests + full sln
  Then: 6/6 PASS; 1197/1197 PASS; Baltic hash unchanged

Test: Detection trial ordering stable
  Given: Baltic harness with fixed seed
  When: Two runs of detection tick output
  Then: Identical contact transition ordering
```

## Test Evidence Path

- `src/ProjectAegis.Sim.Tests/Sensors/PdDetectionContactSimulatorTests.cs` (extend if needed)
- `src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs`
- `tests/regression/replay-golden-*.txt` (unchanged production 6/6 set)

## Out of Scope

- DecisionLog / Datalink LINQ (S35-10)
- DOTS/ECS migration
- 5k-entity scale benchmarks