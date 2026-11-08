# S27-05 story-done — ADR-009 Bounded Validators

**Story:** `production/epics/sprint-27-adr009-bounded/story-027-05-adr009-validators.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| DeterministicDamageApplyBatch ≥4 tests | `DeterministicDamageApplyBatchTests` 4/4 | COVERED |
| AirAspectDomainValidator allow/deny | `AirAspectDomainValidator.cs` + registry tests | COVERED |
| Validator deny → order-log abort code | `DomainValidatorRegistryTests` AIR_ASPECT_BLOCK | COVERED |
| combatDomainsEnabled=false Baltic zero delta | `CombatDomainsEnabled_false_skips_registry` + ReplayGolden 6/6 | COVERED |
| ReplayGolden 6/6 | `ReplayGoldenSuiteTests` | COVERED |
| ZERO touch DelegationBridge | empty diff vs main | COVERED |

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests --filter "Combat|Domain|Damage" -v minimal  # 33/33
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal  # 6/6
```