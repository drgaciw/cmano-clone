# Requirements Traceability Matrix (RTM)

> **Last updated:** 2026-06-02  
> **Scope:** MVP / Baltic vertical slice (requirements docs 13–20 + sensor GDD)  
> **Coverage:** Headless chain GDD → ADR → code → test (see [architecture-review-2026-06-02.md](architecture-review-2026-06-02.md))

## How to read

| Status | Meaning |
|--------|---------|
| COVERED | ADR + implementation + automated test |
| PARTIAL | Implemented subset; design doc scope larger |
| DEFERRED | Explicit MVP deferral with ADR/story note |
| GAP | Not implemented |

## C1–C5 design-review blockers

| ID | Requirement doc | ADR | Implementation | Test evidence | Status |
|----|-----------------|-----|----------------|---------------|--------|
| C1 | 17 Order log / replay | ADR-003 | `DecisionLog`, `ReplayCheckpointStore` | `ReplayGolden*`, `replay-2026-06-02.md` | COVERED |
| C2 | 18 Combat outcomes | ADR-005 | `EngagementOutcomeRecord`, `MessageLogProjection` | `MessageLogProjectionTests`, `MessageLogBridgeTests` | COVERED |
| C2-UI | 18 Message HUD | — | `MessageLogPanelBinder`, `MessageLogPanelHost` | `MessageLogPanelBinderTests` | PARTIAL |
| C3 | 19 ROE / policy | ADR-002 | `PassthroughRoeFilter`, scenario ROE JSON | `PolicyDenialOrderLogTests` | COVERED |
| C4 | 20 EMCON | ADR-002 | `EmconPolicyEvaluator`, `ScenarioEmconResolver` | `EmconPolicyEvaluatorTests`, emcon scenario JSON | COVERED |
| C5 | 13 Human-in-the-loop | ADR-001 | `SimulationModeProfile`, `PlayerOrderRecord` | `PlayModeSmokeHarnessTests` | DEFERRED |

## Sensor / C2 (TR-sensor-001)

| TR-ID | GDD | ADR / note | Story | Test | Status |
|-------|-----|------------|-------|------|--------|
| TR-sensor-001a | sensor-detection-ew | Pd loop | sensor-headless-slice | `BalticReplayHarnessPdDetectionTests` | COVERED |
| TR-sensor-001b | sensor-detection-ew | Classify FSM | sensor-classify-slice | `PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests` | COVERED |
| TR-sensor-001c | sensor-detection-ew | C2 projection | sensor-c2-ui-slice | `SensorC2PanelBinderTests`, `SensorC2BridgeTests` | COVERED |

## Combat / engage spine

| TR-ID | GDD | ADR | Test | Status |
|-------|-----|-----|------|--------|
| TR-combat-001 | combat-outcomes | ADR-005 | `EngagementOrderLogContractTests` | COVERED |
| TR-combat-002 | combat-outcomes | ADR-005 | `ReplayGoldenBalticEngageTests` | COVERED |
| TR-mag-001 | combat-outcomes | — | `ReplayGoldenBalticMagazineTests` | COVERED |

## Uncovered (post-MVP)

| Area | Gap | Suggested action |
|------|-----|------------------|
| Doc 20 full C2 | OOB tree, mission editor, doctrine UI | Sprint 3 `/ux-design` + `/team-ui` |
| Requirements 01–12 | No GDD | `/map-systems` backlog |
| C5 player override | Pause / direct order UX | Story under simulation-control epic |

## Coverage summary

| Status | Count |
|--------|-------|
| COVERED | 11 |
| PARTIAL | 2 |
| DEFERRED | 1 |
| GAP (post-MVP) | 3+ |