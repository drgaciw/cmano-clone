---
id: S33-09
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint33/combat-phase6-regression
estimate_days: 0.5
dependencies:
  - S32-04, S32-05, S32-08, S32-09; S33-04
owner: team-simulation
sprint: 33
req_trace: Req 18; ADR-009 Phase 6
completed: 2026-06-19
---

# Story 033-09 — Phase 6 Integration Smoke (or S32 Carryforward)

> **Epic:** sprint-33-cyber-comms-datalink

## Summary

**Regression-only closeout (2026-06-19):** S32 already proved facility, ECCM, mine transit, and BDA in isolated pins. This story re-runs the existing `Combat|Domain|Facility|Eccm|Mine|Bda` filter suite (115/115) and ReplayGolden 6/6 — **no** new `baltic-patrol-combat-phase6-smoke` combined fixture.

**Path A (combined fixture):** Retired — not required post-S32 per `production/agentic/sprint-33-plan-sim-2026-06-19.md`.

**Path B (S32 carryforward):** Retired — S32-04/05/08/09 ACs already satisfied.

Evidence: `production/qa/smoke-sprint-33-phase6-regression-2026-06-19.md`, `production/agentic/stacks/sprint33/S33-09-DONE.md`.

## Acceptance Criteria

- [x] Isolated golden + `/replay-verify` PASS — S32 pins unchanged; ReplayGolden 6/6
- [x] Not in ReplayGolden 6/6 catalog — no combined Phase 6 fixture added
- [x] `Combat|Domain|Facility|Eccm|Mine|Bda` filters PASS — 115/115

## Verify Commands

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Facility|Eccm|Mine|Bda" -v minimal
/replay-verify
```