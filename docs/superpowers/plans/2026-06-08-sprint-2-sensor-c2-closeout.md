# Sprint 2 Closeout — Sensor Classify + C2 Presentation

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development or dispatching-parallel-agents.

**Goal:** Close Sprint 2 from ~90% to 100% — fill classify→C2 integration test gap, align traceability docs, run `/story-done`, produce evidence.

**Architecture:** Headless-first. Classify FSM on `PdDetectionContactSimulator`; C2 via order-log projection (`ContactPictureProjection` → `SensorC2Bridge`). S1 overlap on shared projection path — GitNexus impact before edits.

**References:**
- [production/sprints/sprint-2-sensor-c2.md](../../../production/sprints/sprint-2-sensor-c2.md)
- [production/agentic/sprint-2-closeout-2026-06-08.md](../../../production/agentic/sprint-2-closeout-2026-06-08.md)
- Gap analysis: [spirit1-vertical-slice-gap-analysis-2026-06-05.md](../../../Game-Requirements/reviews/spirit1-vertical-slice-gap-analysis-2026-06-05.md)

---

## Definition of Done

- [x] All three Sprint 2 stories pass `/story-done` with test-criterion traceability
- [x] `TR-sensor-001a/b/c` consistently COVERED across RTM, `tr-registry.yaml`, GDD review log, implementation tracker
- [x] New integration test: `baltic-patrol-classify` → `SensorC2` shows `Classified`/`Identified`
- [x] Lost-transition `PreviousState` regression (`PdContactStaleTests`)
- [x] Full local gate PASS (430 solution tests, 7 PlayMode)
- [x] Closeout artifacts: this plan + `production/agentic/sprint-2-closeout-2026-06-08.md` + smoke evidence
- [x] `production/sprint-status.yaml` formal `sprint: 2` section

---

## Task 1: Pre-work (orchestrator)

- [x] Baseline build + test gate
- [x] GitNexus analyze (deferred if MCP unavailable; manual blast-radius note for shared symbols)

## Task 2: Agent A — Headless classify spine

- [x] Audit S1/S2 boundary (`ScenarioContactSimulator` seed vs Pd classify promotions)
- [x] Verify `ReplayGoldenBalticClassifyTests` green
- [x] Add `Lost_transition_uses_actual_previous_state_not_hardcoded_detected` in `PdContactStaleTests`

## Task 3: Agent B — C2 bridge/panel tests (TDD)

- [x] `SensorC2BridgeTests.Baltic_patrol_classify_sensor_c2_shows_classified_then_identified`
- [x] `SensorC2PanelBinderIntegrationTests.Baltic_patrol_classify_binder_shows_lifecycle_states`

## Task 4: Agent C — Requirements/traceability docs

- [x] `sensor-detection-ew-review-log.md` — 2026-06-08 re-review
- [x] `tr-registry.yaml` — TR-sensor-001 → covered
- [x] `architecture-traceability-index.md`, `requirements-traceability.md`, implementation tracker, sprint-2 doc

## Task 5: Agent D — story-done + QA

- [x] Test traceability sections on 3 stories
- [x] `production/qa/smoke-sprint2-closeout-2026-06-08.md`

## Task 6: Orchestrator merge

- [x] Full gate PASS
- [x] `production/agentic/sprint-2-closeout-2026-06-08.md`
- [x] `production/sprint-status.yaml` sprint: 2 block