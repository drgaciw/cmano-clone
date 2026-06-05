# Sprint 3 — C2 shell (doc 20 partial)

> **Dates:** 2026-06-03 → 2026-06-17 (proposed)  
> **Goal:** Close RTM gaps C2-UI (full message log) and doc-20 **left drawer** headless projections (OOB + missions); Unity hosts for OOB tab.

## Prerequisites

- [x] Sprint 2 complete — classify FSM, Sensor C2, message log strip
- [x] `dotnet test ProjectAegis.sln` green on `main`
- [x] `/replay-verify` PASS ([replay-2026-06-02](../determinism/replay-2026-06-02.md))

## Committed

| Epic / work item | Stories | Gate |
|------------------|---------|------|
| C2 left drawer projections | [c2-left-drawer-slice](../epics/c2-left-drawer-slice/EPIC.md) | **Complete** |
| Full message log (non-combat filter) | story-001 | **Complete** |
| OOB tree projection + UI host | story-002 | **Complete** |
| Mission list projection | story-003 | **Complete** |

## Definition of done

- `dotnet test ProjectAegis.sln` — 0 failures
- PlayMode smoke + `ReplayGolden` pass
- RTM: C2-UI → COVERED (full log); doc-20 drawer → PARTIAL (OOB + missions tabs, no globe)
- `gitnexus impact` before editing `DecisionLog` / `DelegationOrchestrator`

## Deferred (Sprint 4+)

- Globe map, symbology, right unit panel (doc 20 P0 remainder)
- C5 human-in-the-loop pause/override UX
- `/vertical-slice` formal producer gate