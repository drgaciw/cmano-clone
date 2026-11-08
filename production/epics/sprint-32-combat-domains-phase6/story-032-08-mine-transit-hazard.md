---
id: S32-08
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint32/mine-transit-hazard
estimate_days: 2
dependencies:
  - S32-04 facility validator landed
  - S31-04 mine validator complete
owner: team-simulation
sprint: 32
req_trace: TR-combat-dom-001; Req 18 mine transit hazard; ADR-009 Phase 6 bounded hot-tick
---

# Story 032-08 — Mine Transit Hazard Hot-Tick (Bounded)

> **Epic:** sprint-32-combat-domains-phase6  
> **ADR:** ADR-009 (bounded mine hazard), ADR-001 (deterministic sim), ADR-003 (order log)  
> **GDD:** `design/gdd/combat-domains-damage.md`  
> **QA Classification:** Integration + Logic

## Summary

Scenario **`mineHazard`** zone + seeded placement on isolated **`baltic-patrol-mine-transit-hazard`** fixture. Bounded hot-tick hazard evaluation — **no mine-laying missions**, no full mine danger-area map layer. `/replay-verify` mandatory. Not in ReplayGolden 6/6 catalog.

## Acceptance Criteria

- [x] Scenario policy supports optional `mineHazard` zone definition + seeded placement
- [x] Isolated fixture `baltic-patrol-mine-transit-hazard` exercises transit hazard hot-tick path
- [x] Hazard evaluation deterministic across replays — stable order log
- [x] No mine-laying / mine-clearing missions; no full danger-area map layer
- [x] Sim tests PASS (`Combat|Domain|Mine` filters)
- [x] `/replay-verify` PASS on isolated fixture
- [x] Fixture **not** in ReplayGolden 6/6 catalog; production Baltic hash unchanged
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Mine transit hazard hot-tick
  - Given: isolated fixture with `mineHazard` zone and seeded mine placement
  - When: platform transits hazard zone across N ticks
  - Then: hazard outcomes emitted deterministically in order log; no world mutation on flag-off path
  - Edge cases: platform skirts zone boundary; multiple platforms same tick; empty hazard zone

- **AC-2**: Golden catalog isolation
  - Given: ReplayGolden 6/6 default catalog without mine-transit fixture
  - When: ReplayGolden suite runs
  - Then: 6/6 PASS; mine-transit fixture excluded from golden pins
  - Edge cases: accidental fixture registration in golden catalog; hash drift on production Baltic

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Mine" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `MineAspectDomainValidator` | HIGH — READ |
| `CatalogDamageHotTickApplier` | HIGH |
| `DomainValidatorRegistry` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S31-04 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-04-mine-validator.md`
- GDD: `design/gdd/combat-domains-damage.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-08)
- Track plan: `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*