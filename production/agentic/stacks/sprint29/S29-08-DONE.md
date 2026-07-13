# S29-08 story-done — Begin Execution UX

**Story:** `production/epics/sprint-29-c2-core-loop/story-029-08-begin-execution.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| C2 top bar exposes Begin Execution while Planning | `C2TopBarPanel.uxml` + `C2TopBarPanelHost.cs`; `C2TopBarBeginExecutionTests.C2_top_bar_panel_wires_begin_execution_button_to_bridge_host` | COVERED |
| Button invokes `DelegationBridgeHost.BeginExecution()` | Host calls `bridgeHost.BeginExecution()` (not `Bridge.BeginExecution()`); source assertion test | COVERED |
| Phase transition appends `ModeChange` row | `OrderLogC1RowTypesTests.BeginExecution_appends_mode_change_row`; `C2TopBarBeginExecutionTests.BeginExecution_transitions_planning_to_executing_via_bridge` | COVERED |
| Score/loss counters frozen until execution | `C2TopBarProjection` Planning branch; `C2TopBarProjectionTests.Project_freezes_score_counters_while_planning`; `C2TopBarBeginExecutionTests.Planning_top_bar_projection_freezes_score_until_execution` | COVERED |
| Ticks no-op while Planning | `SimulationSessionPhaseTests.Tick_is_no_op_while_planning`; `C2TopBarBeginExecutionTests.Planning_ticks_no_op_until_begin_execution_like_top_bar_button` | COVERED |
| Double-click guard | `C2TopBarBeginExecutionTests.BeginExecution_double_call_appends_single_mode_change_row`; button hidden/disabled when not Planning | COVERED |
| PlayMode harness phase transition tests PASS | `PlayModeSmokeHarnessTests` (15) + `C2TopBarBeginExecutionTests` (5) = 20 under filter | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff | COVERED |

## Score freeze enforcement

`C2TopBarProjection.Project` checks `SimulationPhase.Planning` and emits `FormatFrozenScoreLine(baseScore)` (`SCORE: {base}  KILLS: 0  MSLS: 0`) without calling `LossesScoringProjection`. Live tally resumes only after `BeginExecution` when phase is `Executing`. `DelegationBridgeHost.RunTick` passes `Bridge.Phase` into projection each frame.

## Files changed

- `unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uxml` — Begin Execution button
- `unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uss` — button styles
- `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` — wire button → `bridgeHost.BeginExecution()`, phase-gated visibility
- `src/ProjectAegis.Delegation/Projection/C2TopBarProjection.cs` — Planning score freeze
- `src/ProjectAegis.Delegation.Tests/Projection/C2TopBarProjectionTests.cs` — freeze test (+1)
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2TopBarBeginExecutionTests.cs` — headless wiring tests (new, 5)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "SimulationSessionPhase|OrderLogC1|ModeChange|C2TopBar" -v minimal
# 10/10 PASS

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|C2TopBar|BeginExecution" -v minimal
# 20/20 PASS

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty
```