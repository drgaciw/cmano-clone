# Epic: Sprint 31 — Closeout & DevOps Hygiene

> **Status:** Ready  
> **Sprint:** 31  
> **Dates:** 2026-10-30 → 2026-11-12  
> **Trunk:** `main` @ `3406bc4` (956/956; ReplayGolden 6/6)

## Goal

Day-1 **full-solution re-baseline** (S31-01 @ ≥956), **closeout hygiene** (replay, GitNexus, tracker, stack prune), and optional **CI/local gate doc refresh** for 996 closeout target per producer decision (GHA billing local-gate advisory permanent).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 01 | [Full-solution re-baseline](story-031-01-full-sln-gate.md) | S31-01 | Config | must-have | 1d | Not Started |
| 12 | [CI/local gate refresh](story-031-12-ci-hygiene.md) | S31-12 | Config | nice-to-have | 0.25d | Not Started |
| 13 | [Closeout hygiene](story-031-13-closeout-hygiene.md) | S31-13 | Config | should-have | 0.5d | Not Started |

## Graphite Stack (merge order)

```
main
 └── stack/sprint31/full-sln-gate              (S31-01) — day-1 gate
      └── … (feature stacks)
           └── stack/sprint31/closeout        (S31-13, S31-12)
```

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- **CRITICAL extend-only:** `CatalogWriteGate` (if touched during closeout doc updates)
- GitNexus analyze @ stack tip on S31-13

## Definition of Done

- [ ] S31-01 day-1 baseline ≥956/956 PASS
- [ ] Closeout smoke doc + GitNexus evidence
- [ ] `stack/sprint30/*` pruned (merged branches)
- [ ] `sprint-status.yaml` closeout counters
- [ ] `/smoke-check sprint` PASS
- [ ] Tracker rows 06/18/21 updated

## References

- S30-13 pattern: `production/epics/sprint-30-closeout-devops/story-030-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md`
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md` *(create before implementation)*