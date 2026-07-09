# ME-W2 Event Graph + AC-7 + Static Analysis Plan

**Parent:** `docs/superpowers/plans/2026-07-08-mission-editor-phase2-completion-plan.md`  
**Story:** ME-003  
**Orchestration:** dispatching-parallel-agents + subagent-driven-development

## Tracks

| Track | Owner files | Deliverable |
|-------|-------------|-------------|
| W2-a | `EventDebuggerTrace.cs`, EventDebuggerTests | Add `sim_tick`, `sequence_id`, `action_results` to JSON |
| W2-b | `EventStaticAnalyzer.cs`, ValidationRules or Authoring, AnalyzeTcaGraph | Real dead/unreachable/circular/contradictory codes |
| W2-c | Editor Upsert/Remove event, bus, CLI, MCP | `event_add` / `event_update` / `event_delete` |
| W2-d | `EventGraphPresenter.cs` + tests | Headless nodes/edges |
| W2-e/f | StubScopePinTests, EventDebuggerTests, doc 11 | AC-7 green; stub pins updated |

## Codes (W2-b)

- `EVENT_DEAD_TRIGGER` — conditions empty and TriggerType not Time (authoring never fires)
- `EVENT_UNREACHABLE_ACTION` — ActivateMission mission id not in Missions
- `EVENT_CONTRADICTORY` — same event has Result=true and Result=false conditions
- `EVENT_CIRCULAR` — ActivateMission graph cycle among events (A activates mission referenced by B's MissionComplete, and B activates A's)

Keep ADR-016 EventGraphComplexityRule.

## Invariants

ZERO DelegationBridge; hash; ReplayGolden; test floor monotonic.
