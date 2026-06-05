# Story 001 — Classify / Identify FSM

**Status:** Complete
**Epic:** [sensor-classify-slice](EPIC.md)

## Acceptance

- [x] `ScenarioContactLifecycle` exposes `classifyAfterTicks` / `identifyAfterTicks` (0 = disabled)
- [x] `PdDetectionContactSimulator` emits promotions on sustained detections
- [x] Unit tests in `PdContactClassifyTests`
- [x] `dotnet test ProjectAegis.sln` green (183 pass)