# Story ME-003 — Event graph + AC-7 + static analysis (ME-W2)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W2  
**Status:** Complete (2026-07-08)  
**Type:** Feature (AME-5.5/5.7, AC-7)  

## Acceptance

1. [x] Event debugger JSON includes full projection fields (not stub-only); AC-7 checkbox green  
2. [x] Static analysis: dead / unreachable / circular / contradictory with tests  
3. [x] Event CRUD CLI/MCP if gaps remain  
4. [x] Headless event graph view model (`AnalyzeTcaGraph` graph nodes + findings; dedicated `EventGraphPresenter` / Unity visual graph residual)  
5. [x] StubScope pins updated honestly; TeleportUnit AC-11 unchanged  

## Closeout notes (ME-W2 / W2-e–f docs honesty)

| Track | Outcome | Evidence |
|-------|---------|----------|
| **W2-a** | Full AC-7 projection | `EventDebuggerTrace` emits `sim_tick`, `sequence_id`, `action_results`; `EventDebuggerTests` + fixture `event-no-fire.scenario.json` |
| **W2-b** | Real static analysis codes | `EventStaticAnalyzer` → `EVENT_DEAD_TRIGGER`, `EVENT_UNREACHABLE_ACTION`, `EVENT_CONTRADICTORY`, `EVENT_CIRCULAR`; `EventStaticAnalyzerTests` |
| **W2-c** | Event CRUD | Editor/bus + CLI/MCP `event_add` / `event_update` / `event_delete`; `EventCrudCliTests`, `ScenarioEventCrudEditorTests` |
| **W2-d** | Headless graph surface | `AnalyzeTcaGraph()` node/findings string; Unity visual event-graph **deferred** (product chrome residual) |
| **W2-e** | AC-7 green | Doc 11 AC-7 `[x] Met`; StubScope pins full projection field types |
| **W2-f** | Maturity honesty | AME-5.5 / 5.7 → Partial+ / Shipped headless; implementation mapping updated |

**Doc:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` — AC-7 Met; AME-5.5/5.7 Partial+ headless.  
**Not claimed:** Unity event-graph `EditorWindow`, full sim event runtime for every AME-5.2–5.4 type, Phase 3 Lua.

## Ownership

- Debugger (W2-a): `EventDebuggerTrace`  
- Static analysis (W2-b): `EventStaticAnalyzer`  
- CRUD (W2-c): Editor/bus + `EventCommands`  
- Docs honesty (W2-e/f): doc 11 + this story + epic ME-003  
