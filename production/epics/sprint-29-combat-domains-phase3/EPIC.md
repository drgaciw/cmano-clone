# Epic: Sprint 29 — Combat Domains Phase 3

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`, `design/gdd/sensor-detection-ew.md`  
> **Req:** 18 Combat Domains; TR-sensor-004 (datalink)

## Goal

Enable **`combatDomainsEnabled=true`** on isolated Baltic golden (S29-05), optionally extend **hot-tick catalog damage apply** (S29-09) and **datalink side picture** (S29-11) — while keeping `combat-domains-smoke` on a separate pin and **`combatDomainsEnabled=false` on Baltic until S29-05 lands**.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Baltic combat enable; flag-gated fixtures |
| ADR-003 | Accepted | Order-log abort codes |
| ADR-001 | Accepted | Deterministic sim; `/replay-verify` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01 — shared day-1)
      └── stack/sprint29/combat-baltic-enable   (S29-05)
           ├── stack/sprint29/hot-tick-damage   (S29-09 — nice)
           └── stack/sprint29/datalink-side-picture (S29-11 — nice)
```

**Baseline until S29-05:** `combatDomainsEnabled=false` on Baltic production fixtures (inherited S28 policy).

**Minimum shippable (beyond must-have):** S29-05 + S29-13 closeout.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 05 | [Combat domains Baltic enablement](story-029-05-baltic-combat-enable.md) | S29-05 | Integration | should-have | 1.5d | Not Started |
| 09 | [Damage hot-tick apply (bounded)](story-029-09-hot-tick-damage.md) | S29-09 | Integration | nice-to-have | 1.5d | Not Started |
| 11 | [Datalink side picture (TR-sensor-004)](story-029-11-datalink-side-picture.md) | S29-11 | Integration | nice-to-have | 1d | Not Started |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus / Replay Rules

- `/replay-verify` mandatory on S29-05, S29-09 sim merges
- **`combatDomainsEnabled=false`** on Baltic until S29-05 merges; then flag-on on **isolated Baltic golden** only
- **`combat-domains-smoke`** fixture stays on separate pin — do not conflate with Baltic ReplayGolden 6/6
- **ZERO touch:** `DelegationBridge.cs`
- **No hot-path SQLite** — catalog reads via approved snapshot path only

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S29-09 (hot-tick damage) | S29-05 Baltic enable |
| 4 | S29-11 (datalink) | S29-02 TL export |

## Definition of Done

- [ ] S29-05 complete; new Baltic golden hash pinned; ReplayGolden 6/6
- [ ] Production Baltic fixture policy documented (`combatDomainsEnabled` flip evidence)
- [ ] `combat-domains-smoke` remains isolated on separate pin
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S28-05 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-05-surface-validators.md`
- S28-08 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-08-damage-consumer-wire.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*