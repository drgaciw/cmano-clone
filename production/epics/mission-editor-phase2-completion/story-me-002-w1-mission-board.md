# Story ME-002 — Mission Board P2.2 (ME-W1)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W1  
**Status:** Complete (2026-07-08)  
**Type:** Feature (AME-3.4)  

## Acceptance

1. [x] Filter/sort missions by type (and side when present)
2. [x] Clone mission → new id, editVersion bump, undoable
3. [x] Template/wizard creates Patrol/Strike/Support/Ferry via editor APIs
4. [x] CLI/MCP verbs + manifest parity
5. [x] Headless `MissionBoardPresenter`; optional Unity panel **deferred**
6. [x] Live findings refresh after board mutations

## Defer note (Task 7)

Unity `ScenarioMissionBoardWindow` is **deferred**. Headless `MissionBoardPresenter` + CLI/MCP verbs ship the AME-3.4 backend (list/filter/clone/template + live findings). Acceptance 1–6 are **conceptually met** for the headless/CLI path; the optional Unity UI panel is residual product chrome, not a W1 blocker.

## Ownership

- `ScenarioDocumentEditor` / bus: single W1-a owner  
- CLI: W1-b  
- UA Authoring: W1-c  
