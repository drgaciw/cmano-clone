# S32-04 story-done — Facility Aspect Domain Validator (Bounded)

**Story:** `production/epics/sprint-32-combat-domains-phase6/story-032-04-facility-validator.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/facility-validator`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `FacilityAspectDomainValidator` registered in `DomainValidatorRegistry` | `DomainValidatorRegistry.MvpStubs` + stable ordinal order test | COVERED |
| Allow in envelope; deny with `FireAbortReason.FacilityAspectBlock` | `CombatDomainValidatorTests` allow/deny + boundary | COVERED |
| Validator deny → order-log `FACILITY_ASPECT_BLOCK` (flag-on isolated path) | `DomainValidatorRegistryTests` resolver + manifest mapping | COVERED |
| Production Baltic — zero abort delta; world hash unchanged | `Baltic_flag_off_facility_domain_zero_delta_*`; `BalticCombatDomainsPolicyTests` pin `17144800277401907079` | COVERED |
| Sim tests PASS (`Combat\|Domain\|Facility`) | 69/69 PASS | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | default catalog unchanged | COVERED |
| `/replay-verify` PASS on sim merge | ReplayGolden 6/6 + facility hot-tick 4/4 | COVERED |
| ZERO touch `DelegationBridge` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 Facility validator allow/deny | `Facility_aspect_validator_*`, resolver flag-on deny | COVERED |
| AC-2 Registry stable iteration order | `Registry_iterates_validators_in_stable_domain_order` (Air→Facility) | COVERED |
| AC-3 Baltic flag-off regression | `Baltic_flag_off_facility_domain_zero_delta_*`; ReplayGolden 6/6 | COVERED |

## Registry order verification

`DomainValidatorRegistry.MvpStubs` stable ordinal order:

1. `CombatDomain.Air` — `AirAspectDomainValidator`
2. `CombatDomain.Surface` — `SurfaceAspectDomainValidator`
3. `CombatDomain.Subsurface` — `SubsurfaceAspectDomainValidator`
4. `CombatDomain.Land` — `LandAspectDomainValidator`
5. `CombatDomain.Mine` — `MineAspectDomainValidator`
6. `CombatDomain.Facility` — `FacilityAspectDomainValidator`

## Files changed

- `src/ProjectAegis.Sim/Engage/FacilityAspectDomainValidator.cs` — new
- `src/ProjectAegis.Sim/Engage/EngageContext.cs` — `FacilityAspectInEnvelope`
- `src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs` — register facility validator
- `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` — `MapDomainDenial` for `FacilityAspectBlock`
- `src/ProjectAegis.Sim/Policy/FireAbortReason.cs` — `FacilityAspectBlock`
- `src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs` — `FacilityAspectBlock`
- `data/glossary/abort_reason_manifest.json` — `FACILITY_ASPECT_BLOCK`
- `src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs` — codegen constants
- `src/ProjectAegis.Sim.Tests/Engage/CombatDomainValidatorTests.cs` — facility allow/deny/boundary
- `src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs` — registry, resolver, order-log, flag-off

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Facility" -v minimal   # 69/69 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal   # 6/6 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "BalticCombatDomainsPolicyTests" -v minimal   # 8/8 PASS (world hash 17144800277401907079)
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` (production policy unchanged; `combatDomainsEnabled=true` per S30-09)
- Facility hot-tick world mutation (S31-05 scope)

## Unblocks

- **S32-05** — ECCM scenario factor
- **S32-08** — mine transit hazard
- Phase 6 combat-domain closeout stories