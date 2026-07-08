# Story ME-001 — P2.1 residual host + doc honesty (ME-W0)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W0  
**Status:** Ready (W0-c docs honesty done 2026-07-08; host/QA tracks open)  
**Type:** Unity host + docs  

## Context

P2.1 shipped headless map ORBAT/RP + live findings + CLI. Unity EditorWindow was deferred. Doc 11 previously understated AME-4.2/4.3 maturity (“map not started” / only AME-4.1).

## Acceptance

1. `ScenarioMapAuthoringWindow` (or equivalent) under `unity/ProjectAegis/Assets/Editor/` binds only to Data/Authoring APIs — no Validation rules, no `DelegationBridge`
2. Checklist `production/qa/scenario-editor-p2-1-editor-host-checklist.md` updated with PASS steps or honest DEFERRED-with-reason if Editor unavailable
3. Doc 11 AME-4.2/4.3 + Implementation Mapping reflect headless-shipped map mutations + live findings; no “map not started”
4. Headless Authoring tests still green; no invariant breaks

## Tracks

| Track | Owner | Deliverable | Notes |
|-------|-------|-------------|-------|
| W0-a | Unity/C# | EditorWindow | Residual — full Edit Mode map GUI host |
| W0-b | Test | Authoring test glue if needed | Headless suite already: `ScenarioOrbatReferencePointEditorTests`, `MapAuthoringSurfaceTests`, `ScenarioMapAuthoringIntegrationTests`, `OrbatReferencePointCliTests` |
| W0-c | Docs | Doc 11 honesty | **Done 2026-07-08** — AME-4.2/4.3 **Phase 2 Partial+ (headless)**; Implementation Mapping rows for editor/session/bus/surfaces/CLI; AC-7 stays Partial; AC-8 stays Met; Phase 3 not shipped; no “map not started” |
| W0-d | QA | Checklist evidence | Open |

### W0-c honesty summary (Doc 11)

Canonical path: `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`.

| Area | Maturity wording |
|------|------------------|
| AME-4.2 | Phase 2 Partial+ headless — `UpsertOrbatUnit` / `MoveOrbatUnit` / `CloneOrbatUnit` + CLI + tests + headless `MapAuthoringSurface` / `EditModeController`; Unity map GUI residual ME-W0 |
| AME-4.3 | Phase 2 Partial+ headless — `UpsertReferencePoint` / `TryRemoveReferencePoint` + `reference_point_upsert` + tests; snap/measure/invalid-draw chrome residual |
| Session/bus/findings | `ScenarioAuthoringSession`, `ScenarioEditCommandBus`, `LiveFindingsPresenter` — P2.1 headless shipped |
| AC-7 | **Partial** (stub) — do not flip green |
| AC-8 | **Met** (host load path) — keep met; map GUI not claimed |
| Phase 2 row | **In progress (Partial+)** — P2.1 shipped; not Phase 2 complete |
| Phase 3 | **Not started** |
