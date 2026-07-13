# S28-09 story-done — Facility Damage Projection Stub

**Story:** `production/epics/sprint-28-combat-domains-phase2/story-028-09-facility-damage-stub.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Facility damage projection type (order-log/projection-only) | `OrderLogFacilityDamageProjection` + `FacilityPictureProjection.ProjectWithDamage` | COVERED |
| Projection tests PASS in isolated fixtures | `OrderLogFacilityDamageProjectionTests` (7 tests, `combatDomainsEnabled=true`) | COVERED |
| ReplayGoldenSuiteTests 6/6 unchanged | `ReplayGoldenSuiteTests` | COVERED |
| `combatDomainsEnabled=false` on Baltic production fixtures | No bridge/sim hot-path wiring; golden policies unchanged | COVERED |
| No hot-tick world-state damage apply | Projection-only; no `DecisionLog` append / sim kernel mutation | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | COVERED |

## Files changed

- `src/ProjectAegis.Delegation/Projection/FacilityCapacityStates.cs` — Operational / Damaged / Destroyed labels
- `src/ProjectAegis.Delegation/Projection/FacilityDamageChangeRecord.cs` — projection-only capacity transition row
- `src/ProjectAegis.Delegation/Projection/FacilityPictureEntry.cs` — facility capacity picture entry
- `src/ProjectAegis.Delegation/Projection/OrderLogFacilityDamageProjection.cs` — sorted hit/kill → damage changes (BDA mirror)
- `src/ProjectAegis.Delegation/Projection/FacilityPictureProjection.cs` — seed + damage merge
- `src/ProjectAegis.Delegation.Tests/Projection/OrderLogFacilityDamageProjectionTests.cs` — flag-on fixture tests

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests --filter "Combat|Domain|Damage|Facility" -v minimal
# Passed: 53/53

dotnet test src/ProjectAegis.Delegation.Tests --filter "Facility|Bda|Projection" -v minimal
# Passed: 95/95

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Damage\|Facility | 53 |
| ProjectAegis.Delegation.Tests | Facility\|Bda\|Projection | 95 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Verdict

**COMPLETE** — Order-log facility damage projection stub mirrors S27-06 BDA pattern; replay 6/6; no Baltic golden drift; projection-only scope lock honored.