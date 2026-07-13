# Epic: Sprint 28 — Closeout & DevOps Hygiene

> **Status:** Ready  
> **Sprint:** 28  
> **Dates:** 2026-09-18 → 2026-10-01  
> **Trunk:** `main` @ `a93b55e` (741/741; ReplayGolden 6/6)

## Goal

Day-1 **full-solution re-baseline** (S28-01), **closeout hygiene** (replay, GitNexus, tracker, stack prune), and optional **CI/local gate doc refresh** per producer decision (GHA billing local-gate advisory permanent).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 01 | [Full-solution re-baseline](story-028-01-full-sln-gate.md) | S28-01 | Config | must-have | 1d | Complete |
| 12 | [CI/local gate refresh](story-028-12-ci-hygiene.md) | S28-12 | Config | nice-to-have | 0.25d | Not Started |
| 13 | [Closeout hygiene](story-028-13-closeout-hygiene.md) | S28-13 | Config | should-have | 0.5d | Not Started |

## Graphite Stack (merge order)

```
main
 └── stack/sprint28/full-sln-gate              (S28-01) — day-1 gate
      └── … (feature stacks)
           └── stack/sprint28/closeout        (S28-13, S28-12)
```

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- **CRITICAL extend-only:** `CatalogWriteGate` (if touched during closeout doc updates)
- GitNexus analyze @ stack tip on S28-13

## Definition of Done

- [x] S28-01 day-1 baseline ≥741/741 PASS
- [ ] Closeout smoke doc + GitNexus evidence
- [ ] `stack/sprint27/*` pruned (merged branches)
- [ ] `sprint-status.yaml` closeout counters
- [ ] `/smoke-check sprint` PASS
- [ ] Tracker rows 06/18/21 updated

## References

- S27-13 pattern: `production/epics/sprint-27-closeout-devops/story-027-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*