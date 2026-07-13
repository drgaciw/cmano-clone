# Epic: Sprint 27 — ADR-009 Combat Bounded

> **Status:** Ready  
> **Sprint:** 27  
> **Dates:** 2026-09-04 → 2026-09-17  
> **Trunk:** `main` @ `ab30d35`  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`  
> **Req:** 18 Combat Domains

## Goal

Advance **ADR-009** with deterministic damage outcome ordering, one data-driven **air aspect validator**, validator deny → order log traceability, and **producer-approved order-log BDA slice** — without opening full combat runtime or flipping `combatDomainsEnabled` on Baltic production fixtures.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Domain validators + deterministic damage order |
| ADR-003 | Accepted | Order-log abort codes |
| ADR-001 | Accepted | Deterministic sim |

## Graphite Stack

```
main
 └── stack/sprint27/full-sln-gate           (S27-01 — shared day-1)
      └── stack/sprint27/adr009-validators-bda
           ├── S27-05 validators (2d)
           └── S27-06 order-log BDA (1d) — producer APPROVED
                └── S27-16 checklist (0.25d — nice)
```

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 05 | [ADR-009 bounded validators](story-027-05-adr009-validators.md) | S27-05 | Integration | should-have | 2d | Ready |
| 06 | [Order-log BDA slice](story-027-06-order-log-bda.md) | S27-06 | Integration | should-have | 1d | Ready |
| 16 | [ADR-009 checklist closeout](story-027-16-adr009-checklist.md) | S27-16 | Config | nice-to-have | 0.25d | Ready |

## GitNexus / Replay Rules

- `/replay-verify` mandatory on every sim merge
- `combatDomainsEnabled=false` default on Baltic
- Flag-on coverage in **isolated test fixtures** only
- **ZERO touch:** `DelegationBridge.cs`

## Definition of Done

- [ ] S27-05 complete; ReplayGolden 6/6 on default path
- [ ] S27-06 projection tests PASS (producer-approved)
- [ ] No hot-tick world-state damage apply
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- Kickoff: `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`