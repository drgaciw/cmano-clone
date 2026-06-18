# Epic: Sprint 27 — Closeout & DevOps Hygiene

> **Status:** Ready  
> **Sprint:** 27  
> **Dates:** 2026-09-04 → 2026-09-17  
> **Trunk:** `main` @ `ab30d35`

## Goal

Day-1 baseline enforcement (shared S27-01), **closeout hygiene** (replay, GitNexus, tracker, stack prune), and optional **CI billing documentation** per producer decision (local-gate advisory permanent).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 12 | [CI hygiene / GHA billing doc](story-027-12-ci-hygiene.md) | S27-12 | Config | nice-to-have | 0.5d | Ready |
| 13 | [Closeout hygiene](story-027-13-closeout-hygiene.md) | S27-13 | Config | should-have | 0.5d | Ready |

Note: **S27-01** day-1 baseline lives in `sprint-27-cmo-corpus-import` epic (shared gate).

## Definition of Done

- [ ] Closeout smoke doc + GitNexus evidence
- [ ] `stack/sprint26/*` pruned (merged branches)
- [ ] `sprint-status.yaml` closeout counters
- [ ] `/smoke-check sprint` PASS

## References

- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`
- Track plan: `production/agentic/sprint-27-plan-qa-2026-06-18.md`