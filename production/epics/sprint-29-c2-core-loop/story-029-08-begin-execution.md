---
id: S29-08
status: Not Started
type: UI+Integration
priority: should-have
graphite_branch: stack/sprint29/begin-execution
estimate_days: 1
dependencies:
  - S29-01 green baseline
owner: team-unity
sprint: 29
req_trace: Req 02 Core Loop; Req 03 Simulation Modes; TR-simcore-003
---

# Story 029-08 — Begin Execution UX

> **Epic:** sprint-29-c2-core-loop  
> **ADR:** ADR-010 (headless-first), ADR-001 (deterministic phase transitions)  
> **UX:** `design/ux/c2-command-post.md`

## Summary

**Planning→Executing** phase control in C2 top bar / PlayMode harness. Explicit **Begin Execution** player action; score/loss counters frozen until execution. Phase transition tests PASS. Routes through `DelegationBridgeHost.BeginExecution()` — ZERO touch `DelegationBridge`.

## Acceptance Criteria

- [ ] C2 top bar exposes **Begin Execution** control while `SimulationPhase.Planning`
- [ ] `Begin Execution` invokes `DelegationBridgeHost.BeginExecution()` (not direct orchestrator mutation)
- [ ] Phase transition appends `ModeChange` order-log row (`Planning` → `Executing`)
- [ ] Score/loss counters frozen until execution begins
- [ ] Ticks no-op while Planning (`SimulationSessionPhaseTests` regression PASS)
- [ ] PlayMode harness phase transition tests PASS
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Phase transition on Begin Execution
  - Given: scenario loaded in Planning phase
  - When: player clicks Begin Execution in C2 top bar
  - Then: phase becomes Executing; ticks advance; `ModeChange` row in order log
  - Edge cases: double-click guard; button disabled after execution starts

- **AC-2**: Score/loss counters frozen in Planning
  - Given: scenario in Planning with scoring/loss projection wired
  - When: sim time displayed but ticks frozen
  - Then: score and loss counters do not increment until execution
  - Edge cases: queued orders apply FIFO on Begin Execution per `SimulationSession`

- **AC-3**: Headless phase regression
  - Given: `SimulationSessionPhaseTests` baseline
  - When: full delegation test filter runs
  - Then: `Tick_is_no_op_while_planning` and `BeginExecution_allows_ticks_and_advances_sim` PASS
  - Edge cases: `autoBeginOnStart` smoke harness unchanged unless explicitly updated

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "SimulationSessionPhase|OrderLogC1|ModeChange" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DelegationBridgeHost` | MEDIUM — BeginExecution seam |
| `SimulationSession` | HIGH — phase gate |
| `C2TopBarProjection` | LOW |
| `DelegationBridge.cs` | ZERO touch |

## References

- UX spec: `design/ux/c2-command-post.md`
- GDD: `design/gdd/simulation-core-time.md` (TR-simcore-003)
- Req 02: `Game-Requirements/requirements/02-Core-Gameplay-Loop.md`
- Req 03: `Game-Requirements/requirements/03-Simulation-Modes.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-08)
- Track plan: `production/agentic/sprint-29-plan-unity-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*