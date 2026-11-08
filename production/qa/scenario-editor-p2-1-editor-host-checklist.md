# P2.1 Unity Editor Host Checklist (Task 9 — ME-W0)

**Status:** EditorWindow **implemented** (thin host glue). Manual Edit Mode verification still **local** (requires Unity 6.3 Editor + published Plugins).  
**Date:** 2026-07-08  
**Host approach:** A (thin EditorWindow over `ScenarioAuthoringSession` / bus / surface)

## Implemented

| Item | Path / note |
|------|-------------|
| EditorWindow | `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` |
| Menu | `Project Aegis/Scenario Map Authoring` |
| APIs bound | `ScenarioAuthoringSession`, `ScenarioEditCommandBus` (via surface/session), `MapAuthoringSurface`, `LiveFindingsPresenter` |
| UI | Path field; Open / Save / Rebuild / Refresh Findings; unit + RP + findings lists; place unit; optional 3-vertex RP polygon |
| Constraints | **No** Validation Engine rules inlined; **no** `DelegationBridge` references; glue-only |

## When Unity Editor is available (manual smoke)

1. [x] Create `unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs` with menu `Project Aegis/Scenario Map Authoring`.
2. [ ] Open a `*.scenario.json` path via session `Open`. *(local Editor)*
3. [ ] List ORBAT units and reference points from `MapAuthoringSurface.RebuildFromDocument`. *(local Editor)*
4. [ ] Place unit → save → reload file on disk shows unit in `orbat`. *(local Editor)*
5. [ ] Draw 3-vertex polygon → `referencePoints` present. *(local Editor)*
6. [ ] Findings list shows codes from `LiveFindingsPresenter.RefreshImmediate` matching CLI `scenario_validate`. *(local Editor)*
7. [x] Confirm **no** business rules in Editor script (only bind UI to Data/Authoring APIs). *(code review)*
8. [x] Confirm **no** `DelegationBridge` calls from authoring window. *(code review / grep)*

### Local Editor setup notes

```bash
# From repo root — publish netstandard2.1 plugins into Unity (required for compile in Editor)
./tools/copy-delegation-assemblies.ps1
# Open unity/ProjectAegis in Unity Hub 6.3 LTS (6000.3.x)
# Menu: Project Aegis → Scenario Map Authoring
```

Headless CI does **not** compile this EditorWindow; Unity assembly references resolve when Plugins DLLs are present.

## Exit for Task 9 full

- [x] EditorWindow committed  
- [ ] Smoke screenshot or batchmode note *(local Editor)*  
- [ ] Checklist steps 4–6 manual PASS *(local Editor)*  

## Engineering gate note

Headless surface + bus + CLI already satisfy the P2.1 engineering gate. This host is Approach A glue polish; manual Editor steps remain local verification only.
