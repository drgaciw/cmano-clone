# P2.1 Unity Editor Host Checklist (Task 9 — DEFERRED)

**Status:** Deferred — headless Tasks 1–8 ship the testable product without Unity Editor.  
**Date:** 2026-07-08  
**Host approach:** A (thin EditorWindow over `ScenarioAuthoringSession` / bus / surface)

## When Unity Editor is available

1. Create `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` with menu `Project Aegis/Scenario Map Authoring`.
2. Open a `*.scenario.json` path via session `Open`.
3. List ORBAT units and reference points from `MapAuthoringSurface.RebuildFromDocument`.
4. Place unit → save → reload file on disk shows unit in `orbat`.
5. Draw 3-vertex polygon → `referencePoints` present.
6. Findings list shows codes from `LiveFindingsPresenter.RefreshImmediate` matching CLI `scenario_validate`.
7. Confirm **no** business rules in Editor script (only bind UI to Data/Authoring APIs).
8. Confirm **no** `DelegationBridge` calls from authoring window.

## Exit for Task 9 full

- [ ] EditorWindow committed + smoke screenshot or batchmode note  
- [ ] Checklist steps 4–6 manual PASS  

Until then: **headless surface + bus + CLI** satisfy P2.1 engineering gate; host glue is optional polish.
