# Epic: Sprint 32 — Combat Domains Phase 6

> **Status:** Ready  
> **Sprint:** 32  
> **Dates:** 2026-11-13 → 2026-11-26  
> **Trunk:** `main` @ `3406bc4` (Sprint 31 complete; 1006/1006; QA APPROVED)  
> **Layer:** Core / Simulation  
> **GDD:** `design/gdd/combat-domains-damage.md`, `design/gdd/sensor-detection-ew.md`  
> **Req:** 18 Combat Domains

## Goal

Advance **ADR-009 Phase 6** with bounded **facility aspect domain validator** (S32-04 must-have), **ECCM scenario factor** on `ScenarioDetectionTrial` (S32-05 must-have), optional **mine transit hazard hot-tick** (S32-08), and **BDA contact lifecycle sim hook** (S32-09) — while keeping default ReplayGolden pins stable and **`combatDomainsEnabled=false` on production `baltic-patrol.policy.json`**.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-009 | Accepted | Facility validator plug-in; mine transit hazard; BDA lifecycle hook |
| ADR-003 | Accepted | Order-log abort codes; `PlatformDamageChange` / `ContactChange` |
| ADR-001 | Accepted | Deterministic sim; `/replay-verify` |

## Graphite Stack (merge order)

```
main
 └── stack/sprint32/full-sln-gate              (S32-01 — shared day-1)
      ├── stack/sprint32/facility-validator    (S32-04) — must-have
      │    └── stack/sprint32/mine-transit-hazard (S32-08 — should)
      ├── stack/sprint32/eccm-scenario-factor  (S32-05) — must-have
      └── stack/sprint32/bda-lifecycle-hook    (S32-09 — should)
```

**Baseline:** `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` (inherited S31 policy). Isolated flag-on pins remain separate from ReplayGolden 6/6 catalog.

Note: **S32-01** day-1 baseline lives in `sprint-32-closeout-devops` epic (shared gate).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 04 | [Facility aspect domain validator](story-032-04-facility-validator.md) | S32-04 | Integration + Logic | must-have | 1.5d | Not Started |
| 05 | [ECCM scenario factor (bounded)](story-032-05-eccm-scenario-factor.md) | S32-05 | Integration + Logic | must-have | 1.5d | Not Started |
| 08 | [Mine transit hazard hot-tick](story-032-08-mine-transit-hazard.md) | S32-08 | Integration + Logic | should-have | 2d | Complete |
| 09 | [BDA contact lifecycle sim hook](story-032-09-bda-lifecycle-hook.md) | S32-09 | Integration + Logic | should-have | 1.5d | Not Started |

## GitNexus / Replay Rules

- `/replay-verify` mandatory on S32-04, S32-05, S32-08, S32-09 sim merges
- **`combatDomainsEnabled=false`** on production `baltic-patrol.policy.json` — production Baltic hash unchanged
- **Isolated combat pins** stay on separate hashes — do not conflate with ReplayGolden 6/6
- **ZERO touch:** `DelegationBridge.cs` on **all** stories in this epic
- **No hot-path SQLite** — catalog reads via approved snapshot path only

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 3 | S32-09 (BDA lifecycle hook) | S32-04 facility validator |
| 4 | S32-08 (mine transit hazard) | S32-05 ECCM factor |

## Definition of Done

- [x] S32-04 complete; `FacilityAspectDomainValidator` + `FACILITY_ASPECT_BLOCK` registered; `Combat|Domain|Facility` tests PASS; ReplayGolden 6/6 on default path
- [ ] S32-05 complete; `eccmFactor` on `ScenarioDetectionTrial` + policy JSON; `baltic-patrol-jammed` isolated fixture; ReplayGolden 6/6 default
- [x] S32-08 complete (if capacity); `baltic-patrol-mine-transit-hazard` isolated fixture; `/replay-verify` PASS; not in ReplayGolden 6/6 catalog
- [ ] S32-09 complete (if capacity); contact FSM promotes to `Lost` when `damageLevel ≥ 3`; ReplayGolden 6/6 default
- [ ] Tracker row 18 note updated

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S31-04 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-04-mine-validator.md`
- S31-05 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-05-facility-hot-tick.md`
- S31-06 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-06-bda-hot-path.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md`
- Parallel kickoff: `production/agentic/sprint-32-parallel-kickoff-2026-06-18.md`
- Track plan: `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*