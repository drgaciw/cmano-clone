# Epic: Sprint 32 — Closeout & DevOps Hygiene

> **Status:** Ready  
> **Sprint:** 32  
> **Dates:** 2026-11-13 → 2026-11-26  
> **Trunk:** `main` @ `3406bc4` (1006/1006; ReplayGolden 6/6)

## Goal

Day-1 **full-solution re-baseline** (S32-01 @ ≥1006), optional **CI/local gate doc refresh** (S32-12 — S31-12 carryover), and **closeout hygiene** (replay, GitNexus, tracker, stack prune @ ≥1046).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 01 | [Full-solution re-baseline](story-032-01-full-sln-gate.md) | S32-01 | Config | must-have | 1d | Not Started |
| 12 | [CI/local gate refresh](story-032-12-ci-hygiene.md) | S32-12 | Config | nice-to-have | 0.25d | Not Started |
| 13 | [Closeout hygiene](story-032-13-closeout-hygiene.md) | S32-13 | Config | should-have | 0.5d | Not Started |

## Graphite Stack (merge order)

```
main
 └── stack/sprint32/full-sln-gate              (S32-01) — day-1 gate
      └── … (feature stacks)
           └── stack/sprint32/closeout        (S32-13, S32-12)
```

## GitNexus Mandatory Rules

- **ZERO touch:** `DelegationBridge.cs`
- **CRITICAL extend-only:** `CatalogWriteGate` (if touched during closeout doc updates)
- GitNexus analyze @ stack tip on S32-13

## Definition of Done

- [ ] S32-01 day-1 baseline ≥1006/1006 PASS
- [ ] Closeout smoke doc + GitNexus evidence
- [ ] `stack/sprint31/*` pruned (merged branches)
- [ ] `sprint-status.yaml` closeout counters
- [ ] `/smoke-check sprint` PASS
- [ ] Tracker rows 06/18/20/21 updated

## References

- S31-13 pattern: `production/epics/sprint-31-closeout-devops/story-031-13-closeout-hygiene.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*