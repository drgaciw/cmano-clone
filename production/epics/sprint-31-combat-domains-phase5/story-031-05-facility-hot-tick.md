---
id: S31-05
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint31/facility-hot-tick
estimate_days: 2
dependencies: []
owner: team-simulation
sprint: 31
req_trace: Req 18 Combat Domains; S28-09 facility projection stub extension; S30-08 hot-tick HP ledger
---

# Story 031-05 — Facility Combat Hot-Tick

> **Epic:** sprint-31-combat-domains-phase5  
> **ADR:** ADR-009 (bounded damage), ADR-001 (deterministic sim), ADR-003 (order log)  
> **GDD:** `design/gdd/combat-domains-damage.md` (`damageLevel` 0–3)  
> **QA Classification:** Integration + Logic

## Summary

Extend S28-09 **facility damage projection stub** with **hot-tick HP apply** via `CatalogDamageHotTickApplier` / `PlatformHpLedger` pattern from S30-08. Isolated flag-on fixture; `/replay-verify` mandatory. Production Baltic hash unchanged; no hot-path SQLite; no full facility component model.

## Acceptance Criteria

- [x] Facility engagement `Hit` outcomes apply HP delta through `DeterministicDamageApplyBatch` → `PlatformHpLedger`
- [x] `damageLevel` bounded 0–3 per GDD formula on facility hot-tick path
- [x] Extends S28-09 projection stub — order-log + picture projection remain consistent
- [x] Damage sourced from gate-approved catalog snapshot (no hot-path SQLite)
- [x] Sim tests PASS (`Combat|Domain|Damage|Facility` filters)
- [x] `/replay-verify` PASS on isolated facility hot-tick fixture
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path (production Baltic hash unchanged)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Facility hit → HP ledger apply
  - Given: isolated flag-on fixture with `combatDomainsEnabled=true` and facility engagement `Hit` outcomes
  - When: facility hot-tick applier runs post-engage across N ticks
  - Then: `PlatformHpLedger` reflects sorted apply order; facility picture projection consistent with HP state
  - Edge cases: multiple hits same tick; zero resilience facility; missing damage row

- **AC-2**: Production Baltic regression
  - Given: `combatDomainsEnabled=false` production fixtures (ReplayGolden 6/6 catalog)
  - When: ReplayGolden suite runs without facility hot-tick fixture loaded
  - Then: 6/6 PASS; production Baltic world hash unchanged vs S30 closeout
  - Edge cases: accidental facility apply on flag-off path; isolated pin not in golden catalog

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Facility" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogDamageHotTickApplier` | HIGH |
| `DeterministicDamageApplyBatch` | HIGH |
| `PlatformHpLedger` | HIGH |
| `OrderLogFacilityDamageProjection` | HIGH — extend S28-09 only |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-09 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-09-facility-damage-stub.md`
- S30-08 pattern: `production/epics/sprint-30-combat-domains-phase4/story-030-08-hot-tick-hits.md`
- GDD: `design/gdd/combat-domains-damage.md` (damage level formula §4)
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-05)
- Track plan: `production/agentic/sprint-31-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 8/8 passing
**Deviations**: None
**Test Evidence**: `CatalogFacilityDamageHotTickApplierTests` + `OrderLogFacilityDamageProjectionHotTickTests` + `BalticReplayHarnessFacilityHotTickTests`
**Code Review**: Skipped (lean mode)