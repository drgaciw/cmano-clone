# S30-07 story-done — Begin Execution Planning Chrome

**Story:** `production/epics/sprint-30-c2-planning-chrome/story-030-07-planning-chrome.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/planning-chrome`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Map dimmed while `SimulationPhase.Planning` | `C2PlanningChromeProjection`; `MapPlaceholderPanelHost.IsDimmed`; `C2PlanningChromeTests.Map_placeholder_panel_binds_dimmed_state_while_planning` | COVERED |
| Left drawer read-only while Planning | `C2LeftDrawerPanelHost.IsDrawerReadOnly`; tab gate `OnTabChanged`; selection (`SelectUnit`/`SelectContact`) preserved | COVERED |
| Chrome clears on Begin Execution → Executing | `C2PlanningChromeTests.BeginExecution_clears_planning_chrome_projection` | COVERED |
| Begin Execution via `DelegationBridgeHost.BeginExecution()` | `C2PlanningChromeTests.Begin_execution_still_routes_through_bridge_host_seam`; `C2TopBarBeginExecutionTests` regression | COVERED |
| Score/loss counters frozen until execution | `C2PlanningChromeTests.Planning_top_bar_projection_freezes_score_until_execution_regression` | COVERED |
| Ticks no-op while Planning | `SimulationSessionPhaseTests` (3/3 PASS) | COVERED |
| `C2PlanningChromeTests` PASS | 7/7 headless merge authority | COVERED |
| `C2TopBarBeginExecutionTests` regression PASS | 5/5 under combined filter | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## Planning chrome contract

`C2PlanningChromeProjection.Project(phase)` emits:

- `IsMapDimmed == true` and `IsDrawerReadOnly == true` while `SimulationPhase.Planning`
- Both flags clear when phase is `Executing`
- Unity hosts bind each frame from `bridgeHost.Phase` (ADR-010 read-only projection seam)

## Files changed

- `src/ProjectAegis.Delegation/Projection/C2PlanningChromeState.cs` — new DTO
- `src/ProjectAegis.Delegation/Projection/C2PlanningChromeProjection.cs` — phase → chrome flags
- `unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs` — dim overlay + `IsDimmed`
- `unity/ProjectAegis/Assets/Scripts/Runtime/C2LeftDrawerPanelHost.cs` — read-only gate + `IsDrawerReadOnly`
- `unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uxml` — planning dim overlay
- `unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss` — dimmed canvas + overlay styles
- `unity/ProjectAegis/Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uss` — planning read-only chrome
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlanningChromeTests.cs` — new headless suite (7)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "C2PlanningChrome|C2TopBarBeginExecution|PlayModeSmoke" -v minimal
# 29/29 PASS (C2PlanningChrome 7, C2TopBarBeginExecution 5, PlayModeSmoke 17)

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "SimulationSessionPhase" -v minimal
# 3/3 PASS

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# 6/6 PASS

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty
```