# Sprint 2 — Sensor classify + C2 presentation

> **Dates:** 2026-06-03 → 2026-06-17 (proposed)  
> **Goal:** Close TR-sensor-001 remainder (Classify/Identify FSM) and stand up minimal Unity sensor C2 HUD.

## Prerequisites (from Sprint 1)

- [x] `main` @ `1f7423e` — milsim C1 stack merged (#35–#36)
- [x] `sensor-detection-ew` GDD approved for production UI
- [x] Headless contact + Pd + engage spine green (**181** tests)

## Committed

| Epic / work item | Stories | Gate |
|------------------|---------|------|
| Contact Classify/Identify FSM | [sensor-classify-slice](../epics/sensor-classify-slice/EPIC.md) | **Complete** |
| Unity sensor C2 slice | [sensor-c2-ui-slice](../epics/sensor-c2-ui-slice/EPIC.md) | **Complete** (UI Toolkit + ListView; message log strip) |
| Classify demo scenario | `baltic-patrol-classify` JSON | **Complete** (`ReplayGoldenBalticClassifyTests`) |

## Definition of done

- `dotnet test ProjectAegis.sln` — 0 failures
- PlayMode smoke pass
- `gitnexus impact` run before editing `DecisionLog` / `DelegationOrchestrator`
- Epic rows updated in [index.md](../epics/index.md)

## Tracking

- Generate `production/sprint-status.yaml` sprint section via `/sprint-plan new` when stories exist
- Milestone: [vertical-slice-mvp](../milestones/vertical-slice-mvp.md)

## Closeout (2026-06-08)

**Status:** Complete — TR-sensor-001 MVP slice **COVERED**.

### Evidence

| Area | Artifact |
|------|----------|
| Classify FSM | [sensor-classify-slice](../epics/sensor-classify-slice/EPIC.md) — `PdContactClassifyTests`, `ReplayGoldenBalticClassifyTests` |
| C2 projection | [sensor-c2-ui-slice](../epics/sensor-c2-ui-slice/EPIC.md) — `SensorC2BridgeTests`, `SensorC2PanelBinderTests`, `SensorC2PanelHost` |
| Demo scenario | `data/scenarios/baltic-patrol-classify.policy.json` |
| Golden replay | `tests/regression/replay-golden-baltic-classify-2026-06-02.txt` |
| Design review | [sensor-detection-ew-review-log.md](../../design/gdd/reviews/sensor-detection-ew-review-log.md) (2026-06-08 re-review) |
| TR registry | [tr-registry.yaml](../../docs/architecture/tr-registry.yaml) — TR-sensor-001 → `covered` |

### Verification

- `dotnet test ProjectAegis.sln` — **430/430 PASS** @ closeout (2026-06-08)
- PlayMode smoke — **7/7 PASS**
- Sprint 2 scoped filters — **12/12** UnityAdapter, **2/2** Sim classify, **6/6** Delegation projection
- Evidence: [smoke-sprint2-closeout-2026-06-08.md](../qa/smoke-sprint2-closeout-2026-06-08.md), [sprint-2-closeout-2026-06-08.md](../agentic/sprint-2-closeout-2026-06-08.md)
- **TR-sensor-001:** COVERED (classify FSM + C2 projection)
- **TR-sensor-004:** remains deferred (side picture / datalink)