# Epic: Sprint 30 — Combat Domains Phase 4

> **Status:** Ready  
> **Sprint:** 30  
> **Dates:** 2026-10-16 → 2026-10-29  
> **Trunk:** `main` @ `3406bc4` (878/878; ReplayGolden 6/6; S30-01 day-1 green)  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`, `design/gdd/sensor-detection-ew.md`  
> **Req:** 18 Combat Domains; TR-sensor-004 (datalink lag)

## Goal

Advance **ADR-009 Phase 4** with bounded **land aspect domain validator** (S30-05), **hot-tick engagement `Hit` → `PlatformHpLedger`** with `damageLevel` 0–3 (S30-08), optional **production Baltic `combatDomainsEnabled` flip** producer-gated (S30-09), and bounded **datalink share lag** (S30-10) — while keeping default ReplayGolden pins stable and **`combatDomainsEnabled=false` on production `baltic-patrol.policy.json` until producer approves S30-09**.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Land validator plug-in; deterministic damage order; Baltic flag migration step 4 |
| ADR-003 | Accepted | Order-log abort codes; `PlatformDamageChange` / `ContactChange` |
| ADR-001 | Accepted | Deterministic sim; `/replay-verify` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint30/full-sln-gate              (S30-01 — shared day-1)
      ├── stack/sprint30/combat-land-validator  (S30-05)
      │    ├── stack/sprint30/hot-tick-hits     (S30-08 — should)
      │    └── stack/sprint30/baltic-flag-flip  (S30-09 — nice, producer-gated)
      └── stack/sprint30/datalink-lag           (S30-10 — nice, parallel from S30-01)
```

**Baseline until S30-09 (producer approval):** `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` (inherited S29 policy). Isolated flag-on pins (`baltic-patrol-combat-domains`, hot-tick, datalink fixtures) remain separate from ReplayGolden 6/6 catalog.

**Minimum shippable (beyond must-have):** S30-05 + S30-13 closeout.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 05 | [Land aspect domain validator](story-030-05-land-validator.md) | S30-05 | Integration + Logic | should-have | 1.5d | Not Started |
| 08 | [Hot-tick Hit → HP ledger](story-030-08-hot-tick-hits.md) | S30-08 | Integration + Logic | should-have | 1.5d | Not Started |
| 09 | [Production Baltic flag flip](story-030-09-baltic-flag-flip.md) | S30-09 | Integration | nice-to-have | 0.5d | Not Started |
| 10 | [Datalink share lag](story-030-10-datalink-lag.md) | S30-10 | Integration + Logic | nice-to-have | 1.5d | Not Started |

Note: **S30-01** day-1 baseline lives in `sprint-30-closeout-devops` epic (shared gate).

## GitNexus / Replay Rules

- `/replay-verify` mandatory on S30-05, S30-08, S30-09 sim merges
- **`combatDomainsEnabled=false`** on production `baltic-patrol.policy.json` until S30-09 merges **and** producer approves flag flip
- **`combat-domains-smoke`** and isolated combat pins stay on separate hashes — do not conflate with ReplayGolden 6/6
- **ZERO touch:** `DelegationBridge.cs` on **all** stories in this epic
- **No hot-path SQLite** — catalog reads via approved snapshot path only

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S30-10 (datalink lag) | S30-05 land validator |
| 4 | S30-08 (hot-tick hits) | S30-02 TL Phase 3 |
| — | S30-09 (Baltic flip) | Producer-gated; drop if no sign-off |

## Definition of Done

- [ ] S30-05 complete; `LandAspectDomainValidator` registered; `Combat|Domain` tests PASS; ReplayGolden 6/6 on default path
- [ ] S30-08 complete (if capacity); `Hit` outcomes apply to `PlatformHpLedger` with `damageLevel` 0–3; `/replay-verify` PASS on isolated fixture
- [ ] S30-09 (if producer approves); production Baltic policy documented; isolated pins unchanged
- [x] S30-10 (if capacity); `datalink.shareLagTicks` bounded; deterministic merge order preserved; ReplayGolden 6/6
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S28-05 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-05-surface-validators.md`
- S29-09 pattern: `production/epics/sprint-29-combat-domains-phase3/story-029-09-hot-tick-damage.md`
- S29-11 pattern: `production/epics/sprint-29-combat-domains-phase3/story-029-11-datalink-side-picture.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md`
- Parallel kickoff: `production/agentic/sprint-30-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`