# Epic: Sprint 28 — Logistics Catalog Bridge

> **Status:** Ready  
> **Sprint:** 28  
> **Dates:** 2026-09-18 → 2026-10-01  
> **Trunk:** `main` @ `a93b55e` (741/741; ReplayGolden 6/6)  
> **Layer:** Data + Simulation  
> **GDD:** `design/gdd/` (logistics / engage readiness)  
> **Req:** 16 Logistics

## Goal

Close the **Req 16 gap** — bridge catalog loadout/magazine data to **live magazine counts** for engage readiness and validation — without direct SQLite writes outside `CatalogWriteGate` or hot-tick world mutation.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free catalog read path |
| ADR-011 | Accepted | Magazine data sourced via catalog, not ad-hoc DB writes |
| ADR-001 | Accepted | Deterministic readiness evaluation |

## Graphite Stack (merge order)

```
main
 └── stack/sprint28/full-sln-gate              (S28-01)
      └── stack/sprint28/corpus-golden         (S28-03 — data prerequisite)
           └── stack/sprint28/live-magazines   (S28-06)
```

**Dependency:** S28-06 blocked on S28-03 (platform corpus golden hygiene must land first).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 06 | [Live magazine counts from catalog](story-028-06-live-magazines.md) | S28-06 | Integration | should-have | 1.5d | Not Started |

Note: **S28-01** day-1 baseline lives in `sprint-28-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate` — no direct SQLite writes outside gate
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, loadout/magazine browse projection types
- **Read path only** for magazine counts — writes remain propose→approve via gate

## Should-Have Cut Line

Cut order 3: drop S28-06 before S28-03 corpus hygiene if capacity tight.

## Definition of Done

- [ ] Readiness tests PASS with catalog-sourced magazine counts
- [ ] No direct SQLite writes outside write gate
- [ ] ReplayGolden 6/6 unchanged on default path
- [ ] Tracker row 16 gap note updated

## References

- S27-03 pattern: `production/epics/sprint-27-cmo-corpus-import/story-027-03-loadout-magazine-import.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*