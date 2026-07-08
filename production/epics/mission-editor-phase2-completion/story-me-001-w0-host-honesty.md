# Story ME-001 — P2.1 residual host + doc honesty (ME-W0)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W0  
**Status:** Ready  
**Type:** Unity host + docs  

## Context

P2.1 shipped headless map ORBAT/RP + live findings + CLI. Unity EditorWindow was deferred. Doc 11 may still understate AME-4.2/4.3 maturity.

## Acceptance

1. `ScenarioMapAuthoringWindow` (or equivalent) under `unity/ProjectAegis/Assets/Editor/` binds only to Data/Authoring APIs — no Validation rules, no `DelegationBridge`
2. Checklist `production/qa/scenario-editor-p2-1-editor-host-checklist.md` updated with PASS steps or honest DEFERRED-with-reason if Editor unavailable
3. Doc 11 AME-4.2/4.3 + Implementation Mapping reflect headless-shipped map mutations + live findings; no “map not started”
4. Headless Authoring tests still green; no invariant breaks

## Tracks

| Track | Owner | Deliverable |
|-------|-------|-------------|
| W0-a | Unity/C# | EditorWindow |
| W0-b | Test | Authoring test glue if needed |
| W0-c | Docs | Doc 11 honesty |
| W0-d | QA | Checklist evidence |
