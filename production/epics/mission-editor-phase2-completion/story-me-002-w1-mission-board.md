# Story ME-002 — Mission Board P2.2 (ME-W1)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W1  
**Status:** Pending ME-W0  
**Type:** Feature (AME-3.4)  

## Acceptance

1. Filter/sort missions by type (and side when present)
2. Clone mission → new id, editVersion bump, undoable
3. Template/wizard creates Patrol/Strike/Support/Ferry via editor APIs
4. CLI/MCP verbs + manifest parity
5. Headless `MissionBoardPresenter`; optional Unity panel
6. Live findings refresh after board mutations

## Ownership

- `ScenarioDocumentEditor` / bus: single W1-a owner  
- CLI: W1-b  
- UA Authoring: W1-c  
