# Epic: Sprint 31 — Combat Domains Phase 5

> **Status:** Ready  
> **Sprint:** 31  
> **Dates:** 2026-10-30 → 2026-11-12  
> **Trunk:** `main` @ `3406bc4` (Sprint 30 complete; 956/956; QA APPROVED)  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`, `design/gdd/sensor-detection-ew.md`  
> **Req:** 18 Combat Domains

## Goal

Advance **ADR-009 Phase 5** with bounded **mine aspect domain validator** (S31-04 must-have), optional **facility combat hot-tick** HP apply extending S28-09 projection stub (S31-05), and bounded **BDA hot path** mapping Hit/`damageLevel` → contact status in BDA projections (S31-06) — while keeping default ReplayGolden pins stable and **`combatDomainsEnabled=false` on production `baltic-patrol.policy.json`**.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Mine validator plug-in; facility hot-tick; bounded BDA hot path |
| ADR-003 | Accepted | Order-log abort codes; `PlatformDamageChange` / `ContactChange` |
| ADR-001 | Accepted | Deterministic sim; `/replay-verify` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint31/full-sln-gate              (S31-01 — shared day-1)
      ├── stack/sprint31/mine-validator        (S31-04) — must-have
      │    ├── stack/sprint31/facility-hot-tick (S31-05 — should)
      │    └── stack/sprint31/bda-hot-path      (S31-06 — should)
```

**Baseline:** `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` (inherited S30 policy). Isolated flag-on pins remain separate from ReplayGolden 6/6 catalog.

**Minimum shippable (beyond must-have):** S31-04 + S31-05 + S31-13 closeout.

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 04 | [Mine aspect domain validator](story-031-04-mine-validator.md) | S31-04 | Integration + Logic | must-have | 1.5d | Not Started |
| 05 | [Facility combat hot-tick](story-031-05-facility-hot-tick.md) | S31-05 | Integration + Logic | should-have | 2d | backlog |
| 06 | [BDA hot path (bounded)](story-031-06-bda-hot-path.md) | S31-06 | Integration + Logic | should-have | 1.5d | backlog |

Note: **S31-01** day-1 baseline lives in `sprint-31-closeout-devops` epic (shared gate).

## GitNexus / Replay Rules

- `/replay-verify` mandatory on S31-04, S31-05, S31-06 sim merges
- **`combatDomainsEnabled=false`** on production `baltic-patrol.policy.json` — production Baltic hash unchanged
- **Isolated combat pins** stay on separate hashes — do not conflate with ReplayGolden 6/6
- **ZERO touch:** `DelegationBridge.cs` on **all** stories in this epic
- **No hot-path SQLite** — catalog reads via approved snapshot path only

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S31-06 (BDA hot path) | S31-04 mine validator |
| 2 | S31-05 (facility hot-tick) | S31-03 TL release-train |

## Definition of Done

- [ ] S31-04 complete; `MineAspectDomainValidator` + `MINE_ASPECT_BLOCK` registered; `Combat|Domain` tests PASS; ReplayGolden 6/6 on default path
- [ ] S31-05 complete (if capacity); facility hot-tick HP apply on isolated fixture; `/replay-verify` PASS; production Baltic hash unchanged
- [ ] S31-06 complete (if capacity); Hit/`damageLevel` → contact status in BDA projections; BDA tests PASS; ReplayGolden 6/6 default
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S30-05 pattern: `production/epics/sprint-30-combat-domains-phase4/story-030-05-land-validator.md`
- S30-08 pattern: `production/epics/sprint-30-combat-domains-phase4/story-030-08-hot-tick-hits.md`
- S28-09 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-09-facility-damage-stub.md`
- S27-06 pattern: `production/epics/sprint-27-adr009-bounded/story-027-06-order-log-bda.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md`
- Parallel kickoff: `production/agentic/sprint-31-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-31-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`