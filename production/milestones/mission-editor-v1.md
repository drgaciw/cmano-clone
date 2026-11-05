# Milestone: Agentic Mission Editor v1 (GDD MVP)

## Overview

- **Target Date**: 2026-09-02 (proposed, 6 × 2-week sprints after Sprint 22)
- **Type**: Feature Program
- **Duration**: 12 weeks (Sprints 23–28)
- **Orchestration**: 1 FTE + parallel subagents in git worktrees + Graphite stacks

## Milestone Goal

Deliver the [Agentic Mission & Scenario Editor GDD v1](design/gdd/agentic-mission-editor.md) end-to-end: canonical `scenario.json` / `*.aegis-scenario` package, four mission archetypes, typed events DSL, deterministic Validation Engine, full MCP suite, Unity edit-mode shell, and mechanically testable **AC-1..AC-12**.

**Baseline:** Headless spine exists (ADR-008, ADR-010): `ScenarioValidationEngine`, Patrol/Strike MCP CRUD, `scenario_validate` / `scenario_simulate_sample`, partial `editVersion` guards (~30% v1 scope).

## Success Criteria

- [ ] All AC-1..AC-12 mechanically tested (see epic traceability)
- [ ] `dotnet test ProjectAegis.sln` — 0 failures
- [ ] PlayMode smoke extended for AC-8 (Sprint 27)
- [ ] `/replay-verify` golden for editor sample scenarios (Sprint 28)
- [ ] Req 11 tracker row **MVP Complete** with evidence paths
- [ ] Graphite stack merged to `main` via `gt submit --stack`

## Sprint Map

| Sprint | Dates (proposed) | Theme | Key ACs |
|--------|------------------|-------|---------|
| **23** | 2026-07-08 | Canonical file foundation | AC-6 (writer), AC-9 |
| **24** | 2026-07-22 | Events DSL + determinism | AC-2, AC-7 |
| **25** | 2026-08-05 | Four missions + MCP completion | AC-3, AC-4, AC-5 |
| **26** | 2026-08-19 | Export gates + concurrency | AC-10, AC-11, AC-12 |
| **27** | 2026-09-02 | Unity edit-mode shell | AC-8 |
| **28** | 2026-09-16 | Program hardening + exit | Full regression |

## Feature List

### Must Ship

| Feature | Epic stories | Sprint |
|---------|--------------|--------|
| Full scenario.json schema + ZIP package | ME-001..ME-004, ME-009 | 23 |
| Typed events + fire_order | ME-010..ME-014 | 24 |
| Strike/Patrol/Support/Ferry + geometry MCP | ME-020..ME-025 | 25 |
| Save/export gates + brief export | ME-030..ME-034 | 26 |
| Unity ScenarioEditorHost | ME-040..ME-044 | 27 |
| AC regression + tracker | ME-050..ME-053 | 28 |

### Out of Scope (Phase 2/3)

- NL Mission Planner Agent, Red Force Agent, CMO import, Lua shim
- Operations timeline UI polish, mining/cargo P1 types
- Steam Workshop sharing

## Quality Gates

| Gate | Threshold |
|------|-----------|
| Test suite | 0 failures — `dotnet test ProjectAegis.sln` |
| Determinism | AC-2 + `/replay-verify` on editor fixtures |
| GitNexus | impact before HIGH symbols; detect_changes before commit |
| Hindsight | `dev-story-mission-editor-v1` recall/retain per sprint |

## Dependencies

- Sprint 22 complete (platform editor / DB doctrine)
- GDD design gate APPROVED 2026-06-01 (`agentic-mission-editor.md`)
- ADR-006, ADR-008, ADR-010 Accepted

## Artifacts

- Epic: [`production/epics/agentic-mission-editor-v1/EPIC.md`](../epics/agentic-mission-editor-v1/EPIC.md)
- Execution plan: [`docs/superpowers/plans/2026-06-14-mission-editor-v1-program.md`](../../docs/superpowers/plans/2026-06-14-mission-editor-v1-program.md)
- Hindsight bank: `dev-story-mission-editor-v1`
