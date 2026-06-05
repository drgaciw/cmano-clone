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