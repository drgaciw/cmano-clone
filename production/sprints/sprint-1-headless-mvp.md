# Sprint 1 — Headless MVP (plan → fight → replay)

> **Dates:** 2026-06-02 → 2026-06-16 (proposed)  
> **Goal:** Close content + plan + replay gaps while headless sensor/engage spine stays green.

## Capacity assumption

Single engineer + agents; parallel stacks only when file ownership table allows.

## Committed

| Epic | Stories | Gate |
|------|---------|------|
| [platform-db-basepd-slice](../epics/platform-db-basepd-slice/EPIC.md) | 4 stories | **Complete** |
| [policy-engage-unification-slice](../epics/policy-engage-unification-slice/EPIC.md) | merged with DATA stack | **Complete** |
| P0 DATA docs | on `main` | ADR-006 reviewed |

## Stretch (delivered on `stack/milsim-c1-combat-data-replay`)

| Epic | Status |
|------|--------|
| [mission-runtime-headless-slice](../epics/mission-runtime-headless-slice/EPIC.md) | **Complete** |
| [order-log-replay-checkpoints-slice](../epics/order-log-replay-checkpoints-slice/EPIC.md) | **Complete** |
| [combat-outcomes-mvp-slice](../epics/combat-outcomes-mvp-slice/EPIC.md) | **Complete** |

## Deferred (Sprint 2+)
- C2 / message log UI
- Contact Classify FSM

## Definition of done

- `dotnet test ProjectAegis.sln` — 177 pass (2026-06-02)
- PlayMode smoke pass
- No HIGH GitNexus impact without review
- Epic status updated in [index.md](../epics/index.md)