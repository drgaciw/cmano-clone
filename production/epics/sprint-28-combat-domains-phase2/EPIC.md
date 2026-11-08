# Epic: Sprint 28 — Combat Domains Phase 2

> **Status:** Ready  
> **Sprint:** 28  
> **Dates:** 2026-09-18 → 2026-10-01  
> **Trunk:** `main` @ `a93b55e` (741/741; ReplayGolden 6/6)  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`  
> **Req:** 18 Combat Domains

## Goal

Advance **ADR-009** beyond air aspect with **bounded surface/subsurface domain validators**, extend **damage sim consumer** beyond S25 stub (readiness/withdraw only), and optionally land facility damage projection + balance drift telemetry — without hot-tick world mutation, full mine/land/facility runtime, or `combatDomainsEnabled=true` on Baltic production fixtures.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Surface/subsurface validators; flag-gated fixtures |
| ADR-003 | Accepted | Order-log abort codes |
| ADR-001 | Accepted | Deterministic sim |

## Graphite Stack (merge order)

```
main
 └── stack/sprint28/full-sln-gate              (S28-01 — shared day-1)
      └── stack/sprint28/combat-phase2         (S28-05)
           ├── S28-08 damage consumer wire (1.5d — should-have)
           ├── S28-09 facility damage stub (1d — nice)
           └── S28-10 balance drift consumer (0.5d — nice)
```

**Minimum shippable (beyond must-have):** S28-05 + S28-13 closeout.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 05 | [Surface/subsurface domain validators (bounded)](story-028-05-surface-validators.md) | S28-05 | Integration | should-have | 1.5d | Not Started |
| 08 | [Damage sim consumer wire (beyond stub)](story-028-08-damage-consumer-wire.md) | S28-08 | Integration | should-have | 1.5d | Not Started |
| 09 | [Facility damage projection stub](story-028-09-facility-damage-stub.md) | S28-09 | Logic | nice-to-have | 1d | Not Started |
| 10 | [Balance drift telemetry consumer](story-028-10-balance-drift-consumer.md) | S28-10 | Logic | nice-to-have | 0.5d | Not Started |

Note: **S28-01** day-1 baseline lives in `sprint-28-closeout-devops` epic (shared gate).

## GitNexus / Replay Rules

- `/replay-verify` mandatory on S28-05..08 sim merges
- **`combatDomainsEnabled=false`** default on Baltic production fixtures
- Flag-on coverage in **isolated test fixtures** only
- **ZERO touch:** `DelegationBridge.cs`
- **No hot-tick world-state damage apply** — projection or readiness only

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S28-08 (damage consumer wire) | S28-05 validators |
| 2 | S28-09 (facility stub) | S28-05 surface validator |

## Definition of Done

- [ ] S28-05 complete; ReplayGolden 6/6 on default path
- [ ] `combatDomainsEnabled=false` on Baltic — zero abort delta
- [ ] No hot-tick world-state damage apply
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S27-05 pattern: `production/epics/sprint-27-adr009-bounded/story-027-05-adr009-validators.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md`
- Parallel kickoff: `production/agentic/sprint-28-parallel-kickoff-2026-06-18.md`
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*