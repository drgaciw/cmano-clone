# Story 001 — Classify / Identify FSM

**Status:** Complete
**Epic:** [sensor-classify-slice](EPIC.md)

## Acceptance

- [x] `ScenarioContactLifecycle` exposes `classifyAfterTicks` / `identifyAfterTicks` (0 = disabled)
- [x] `PdDetectionContactSimulator` emits promotions on sustained detections
- [x] Unit tests in `PdContactClassifyTests`
- [x] `dotnet test ProjectAegis.sln` green (183 pass)

## Test traceability

| Acceptance criterion | Test file / method | Notes |
|----------------------|-------------------|-------|
| `ScenarioContactLifecycle` exposes `classifyAfterTicks` / `identifyAfterTicks` (0 = disabled) | `PdContactClassifyTests.Contact_promotes_detected_classified_identified_on_sustained_detections` | Uses `ClassifyAfterTicks: 1`, `IdentifyAfterTicks: 2` |
| Same (0 = disabled) | `PdContactClassifyTests.Default_lifecycle_does_not_emit_classify_or_identify_transitions` | `ScenarioContactLifecycle.Default` → thresholds 0 |
| `PdDetectionContactSimulator` emits promotions on sustained detections | `PdContactClassifyTests.Contact_promotes_detected_classified_identified_on_sustained_detections` | Asserts Detected → Classified → Identified tick sequence |
| Unit tests in `PdContactClassifyTests` | `PdContactClassifyTests` (2 methods above) | `src/ProjectAegis.Sim.Tests/Sensors/PdContactClassifyTests.cs` |
| Integration / golden replay | `ReplayGoldenBalticClassifyTests.Classify_scenario_emits_classified_and_identified_without_engage` | `baltic-patrol-classify` policy JSON + harness fingerprint |
| Lost uses actual `PreviousState` | `PdContactStaleTests.Lost_transition_uses_actual_previous_state_not_hardcoded_detected` | Epic AC #3 |
| `dotnet test ProjectAegis.sln` green | Sprint 2 closeout smoke (`production/qa/smoke-sprint2-closeout-2026-06-08.md`) | **430/430** solution; 12/12 UnityAdapter scoped |