# S28-05 story-done — Surface/Subsurface Domain Validators (Bounded)

**Story:** `production/epics/sprint-28-combat-domains-phase2/story-028-05-surface-validators.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `SurfaceAspectDomainValidator` allow/deny + abort code | `CombatDomainValidatorTests` + `DomainValidatorRegistryTests` | COVERED |
| `SubsurfaceAspectDomainValidator` allow/deny + abort code | `CombatDomainValidatorTests` + `DomainValidatorRegistryTests` | COVERED |
| Validator deny → order-log abort code (flag-on fixtures) | `SURFACE_ASPECT_BLOCK`, `SUBSURFACE_ASPECT_BLOCK` mapping tests | COVERED |
| `combatDomainsEnabled=false` Baltic zero abort delta | `Baltic_flag_off_zero_abort_delta_*` + ReplayGolden 6/6 | COVERED |
| ReplayGolden 6/6 | `ReplayGoldenSuiteTests` | COVERED |
| ZERO touch DelegationBridge | empty diff vs HEAD | COVERED |
| No mine/land/facility combat at full runtime | validators only; registry Air/Surface/Subsurface | COVERED |

## Files changed

- `src/ProjectAegis.Sim/Engage/EngageContext.cs` — `SurfaceAspectInEnvelope`, `SubsurfaceAspectInEnvelope`
- `src/ProjectAegis.Sim/Engage/SurfaceAspectDomainValidator.cs` — new
- `src/ProjectAegis.Sim/Engage/SubsurfaceAspectDomainValidator.cs` — new
- `src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs` — MvpStubs uses real surface/subsurface validators
- `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` — `MapDomainDenial` for new abort reasons
- `src/ProjectAegis.Sim/Policy/FireAbortReason.cs` — `SurfaceAspectBlock`, `SubsurfaceAspectBlock`
- `src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs` — matching engage abort reasons
- `data/glossary/abort_reason_manifest.json` — `SURFACE_ASPECT_BLOCK`, `SUBSURFACE_ASPECT_BLOCK`
- `src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs` — codegen constants
- `src/ProjectAegis.Sim.Tests/Engage/CombatDomainValidatorTests.cs` — surface/subsurface allow/deny
- `src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs` — registry, resolver, order-log, Baltic flag-off

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal  # 44/44 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal  # 6/6 PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
```