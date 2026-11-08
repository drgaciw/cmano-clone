---
id: S30-07
status: Complete
type: UI+Integration
priority: should-have
graphite_branch: stack/sprint30/planning-chrome
estimate_days: 1.5
dependencies:
  - S30-01 green baseline
owner: team-unity
sprint: 30
req_trace: Req 02 Core Loop; Req 20 Command and Control UI; TR-simcore-003
---

# Story 030-07 — Begin Execution Planning Chrome

> **Epic:** sprint-30-c2-planning-chrome  
> **ADR:** ADR-010 (headless-first), ADR-001 (deterministic phase transitions)  
> **UX:** `design/ux/c2-command-post.md` (Planning state §5)

## Summary

Extend **S29-08 Begin Execution UX** with full **Planning-phase chrome**: map dimmed, left drawer read-only while `SimulationPhase.Planning`; chrome lifts on `Begin Execution` via `DelegationBridgeHost.BeginExecution()`. No engage intents during Planning. Headless `C2PlanningChromeTests` are merge authority; ZERO touch `DelegationBridge`.

## Acceptance Criteria

- [x] `MapPlaceholderPanelHost` (or map overlay) applies dimmed visual state while `SimulationPhase.Planning`
- [x] `C2LeftDrawerPanelHost` tabs/controls read-only (no mission/order mutations) while Planning
- [x] Planning chrome clears when phase transitions to `Executing` via Begin Execution
- [x] `Begin Execution` still routes through `DelegationBridgeHost.BeginExecution()` — not direct orchestrator mutation
- [x] Score/loss counters frozen until execution begins (S29-08 regression PASS)
- [x] Ticks no-op while Planning (`SimulationSessionPhaseTests` regression PASS)
- [x] `C2PlanningChromeTests` PASS (new headless suite in `ProjectAegis.Delegation.UnityAdapter.Tests`)
- [x] `C2TopBarBeginExecutionTests` regression PASS
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Map dimmed while Planning
  - Given: scenario loaded in Planning phase
  - When: `C2PlanningChromeProjection` (or host binding) resolves phase
  - Then: map panel reports `IsDimmed == true`; dimmed state clears after Begin Execution
  - Edge cases: phase already Executing on load skips dim; replay mode deferred

- **AC-2**: Drawer read-only while Planning
  - Given: scenario in Planning with left drawer populated (OOB / missions / contacts)
  - When: user attempts tab switch or order mutation controls
  - Then: drawer remains view-only; no engage intents dispatched
  - Edge cases: selection for inspection allowed; command dispatch blocked until Executing

- **AC-3**: Planning chrome lifts on Begin Execution
  - Given: Planning chrome active (map dimmed, drawer read-only)
  - When: player clicks Begin Execution in C2 top bar
  - Then: phase becomes Executing; map undimmed; drawer interactive; `ModeChange` row in order log
  - Edge cases: double-click guard; button disabled after execution starts

- **AC-4**: Score freeze + headless regression
  - Given: `C2TopBarBeginExecutionTests` + `SimulationSessionPhaseTests` baseline
  - When: full delegation test filter runs
  - Then: `Planning_top_bar_projection_freezes_score_until_execution` and `Tick_is_no_op_while_planning` PASS
  - Edge cases: `autoBeginOnStart` smoke harness unchanged unless explicitly updated

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "SimulationSessionPhase|OrderLogC1|ModeChange" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "C2PlanningChrome|C2TopBar|PlayModeSmoke" -v minimal
# Optional Editor capture
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario planning-chrome
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `C2PlanningChromeProjection` | LOW — new read-only projection |
| `MapPlaceholderPanelHost` | LOW — dimmed state binding |
| `C2LeftDrawerPanelHost` | LOW — read-only gate |
| `DelegationBridgeHost` | MEDIUM — BeginExecution seam (unchanged) |
| `C2TopBarPanelHost` | LOW — Begin Execution trigger |
| `SimulationSession` | HIGH — phase gate (read-only from UI) |
| `DelegationBridge.cs` | ZERO touch |

## References

- UX spec: `design/ux/c2-command-post.md` (Planning state §5)
- GDD: `design/gdd/simulation-core-time.md` (TR-simcore-003)
- Req 02: `Game-Requirements/requirements/02-Core-Gameplay-Loop.md`
- Req 20: `Game-Requirements/requirements/20-Command-And-Control-UI.md`
- S29-08 pattern: `production/epics/sprint-29-c2-core-loop/story-029-08-begin-execution.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-07)
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`