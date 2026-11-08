# S30-05 story-done — Land Aspect Domain Validator (Bounded)

**Story:** `production/epics/sprint-30-combat-domains-phase4/story-030-05-land-validator.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/combat-land-validator`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `LandAspectDomainValidator` registered in `DomainValidatorRegistry` | `DomainValidatorRegistry.MvpStubs` + stable ordinal order test | COVERED |
| Allow in envelope; deny with `FireAbortReason.LandAspectBlock` | `CombatDomainValidatorTests` allow/deny + boundary | COVERED |
| Validator deny → order-log `LAND_ASPECT_BLOCK` (flag-on isolated path) | `DomainValidatorRegistryTests` resolver + manifest mapping | COVERED |
| `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` | fixture omits flag (default false); `Baltic_flag_off_zero_abort_delta_*` | COVERED |
| Sim tests PASS (`Combat\|Domain`) | 35/35 PASS | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | default catalog unchanged | COVERED |
| `/replay-verify` PASS on sim merge | ReplayGolden 6/6 + zero abort delta flag-off | COVERED |
| No mine/facility full runtime | land validator only; mine/facility deferred | COVERED |
| ZERO touch `DelegationBridge` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 Land validator allow/deny | `Land_aspect_validator_*`, resolver flag-on deny | COVERED |
| AC-2 Registry stable iteration order | `Registry_iterates_validators_in_stable_domain_order` (Air→Land) | COVERED |
| AC-3 Baltic flag-off regression | `Baltic_flag_off_zero_abort_delta_*`; ReplayGolden 6/6 | COVERED |

## Files changed

- `src/ProjectAegis.Sim/Engage/LandAspectDomainValidator.cs` — new
- `src/ProjectAegis.Sim/Engage/EngageContext.cs` — `LandAspectInEnvelope`
- `src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs` — register land validator
- `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` — `MapDomainDenial` for `LandAspectBlock`
- `src/ProjectAegis.Sim/Policy/FireAbortReason.cs` — `LandAspectBlock`
- `src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs` — `LandAspectBlock`
- `data/glossary/abort_reason_manifest.json` — `LAND_ASPECT_BLOCK`
- `src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs` — codegen constants
- `src/ProjectAegis.Sim.Tests/Engage/CombatDomainValidatorTests.cs` — land allow/deny/boundary
- `src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs` — registry, resolver, order-log, Baltic flag-off

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain" -v minimal   # 35/35 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal   # 6/6 PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` (`combatDomainsEnabled` remains default false until S30-09)
- Mine/facility runtime validators

## Unblocks

- **S30-08** — hot-tick damage Hit → HP extensions
- **S30-09** — production Baltic `combatDomainsEnabled` flip (producer-gated)