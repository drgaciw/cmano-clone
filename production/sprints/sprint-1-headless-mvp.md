# Sprint 1 — Headless MVP (plan → fight → replay)

> **Dates:** 2026-06-02 → 2026-06-16 (proposed)  
> **Goal:** Close content + plan + replay gaps while headless sensor/engage spine stays green.

## Capacity assumption

Single engineer + agents; parallel stacks only when file ownership table allows.

## Committed

| Epic | Stories | Gate |
|------|---------|------|
| [platform-db-basepd-slice](../epics/platform-db-basepd-slice/EPIC.md) | TBD (`/create-stories`) | `dotnet test` + sorted catalog tests |
| [policy-engage-unification-slice](../epics/policy-engage-unification-slice/EPIC.md) | TBD | `/replay-verify` |
| P0 DATA docs | Cherry-pick `ed792ef` | Review ADR-006 |

## Stretch

| Epic | Blocker |
|------|---------|
| [mission-runtime-headless-slice](../epics/mission-runtime-headless-slice/EPIC.md) | C4 + mission runtime GDD |
| [order-log-replay-checkpoints-slice](../epics/order-log-replay-checkpoints-slice/EPIC.md) | C1 order-log union design |

## Deferred (Sprint 2+)

- [combat-outcomes-mvp-slice](../epics/combat-outcomes-mvp-slice/EPIC.md)
- C2 / message log UI
- Contact Classify FSM

## Definition of done

- `dotnet test ProjectAegis.sln` — 129+ pass
- PlayMode smoke pass
- No HIGH GitNexus impact without review
- Epic status updated in [index.md](../epics/index.md)