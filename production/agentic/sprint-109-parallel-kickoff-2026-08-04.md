# Sprint 109 Parallel Kickoff — Agent Attention UI (DRG-67)

**Date:** 2026-08-04  
**Stage:** Release  
**Notion:** [S109 Plan](https://app.notion.com/p/3b2f7cb4e4df8162991adbd3c39ccfe8)  
**Playbook:** `linear-parallel-dispatch-playbook.md` · skill `dispatching-parallel-agents`

## Surface analysis (parallelism safety)

| Lane | Issue | Surface | Co-dispatch? |
|------|-------|---------|--------------|
| A | DRG-77 S109-01 | `Attention/*`, `Projection/AgentAttention*` | Foundation — first |
| B | DRG-78 S109-02 | `AgentRoster*`, Unity `AgentRosterPanelHost` | After A; shares Projection |
| C | DRG-79 S109-05 | `AttentionExplainProjection` | After A; Projection-only |
| D | DRG-80 S109-03 | `AttentionTierAlertProjection` | After A/B; Projection-only |
| E | DRG-81 S109-04 | `AttentionAssignmentForecastProjection` | After A; Projection-only |
| F | DRG-82 S109-06 | docs + gates | After all |

**Decision:** Lanes B–E share `ProjectAegis.Delegation/Projection` — **not surface-disjoint**.  
Per playbook §4, they ship as **one serial PR** rather than concurrent worktrees.

## Delivered contract (this PR)

- Named tiers: `Nominal`, `SlowerReactions`, `NarrowedFocus`, `SimplerDecisions`
- Decision-time projection from `AttentionEvaluation` (no UI re-derive of load)
- Roster apply with accessible labels (not color-only)
- Tier-crossing alerts (transition-only, attributable)
- Assignment forecast (advisory; uses `AttentionCalculator` with hypothetical members)
- Explain snippet from `DecisionRecord` load/budget

## Hard invariants

- No `DelegationBridge.Tick` edits
- No attention rule changes in `AttentionCalculator`
- Replay/hash untouched

