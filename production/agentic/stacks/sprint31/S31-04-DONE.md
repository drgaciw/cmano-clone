# S31-04 story-done — Mine Aspect Domain Validator (Bounded)

**Story:** `production/epics/sprint-31-combat-domains-phase5/story-031-04-mine-validator.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/mine-validator`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `MineAspectDomainValidator` registered in `DomainValidatorRegistry` | `DomainValidatorRegistry.MvpStubs` + stable ordinal order test | COVERED |
| Allow in envelope; deny with `FireAbortReason.MineAspectBlock` | `CombatDomainValidatorTests` allow/deny + boundary | COVERED |
| Validator deny → order-log `MINE_ASPECT_BLOCK` (flag-on isolated path) | `DomainValidatorRegistryTests` resolver + manifest mapping | COVERED |
| `combatDomainsEnabled=false` on production path — zero abort delta | `Baltic_flag_off_mine_domain_zero_delta_*`; ReplayGolden 6/6 | COVERED |
| Sim tests PASS (`Combat\|Domain\|Mine`) | 52/52 PASS | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | default catalog unchanged | COVERED |
| `/replay-verify` PASS on sim merge | ReplayGolden 6/6 + full sln PASS | COVERED |
| ZERO touch `DelegationBridge` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 Mine validator allow/deny | `Mine_aspect_validator_*`, resolver flag-on deny | COVERED |
| AC-2 Registry stable iteration order | `Registry_iterates_validators_in_stable_domain_order` (Air→Mine) | COVERED |
| AC-3 Baltic flag-off regression | `Baltic_flag_off_mine_domain_zero_delta_*`; ReplayGolden 6/6 | COVERED |

## Registry order verification

`DomainValidatorRegistry.MvpStubs` stable ordinal order:

1. `CombatDomain.Air` — `AirAspectDomainValidator`
2. `CombatDomain.Surface` — `SurfaceAspectDomainValidator`
3. `CombatDomain.Subsurface` — `SubsurfaceAspectDomainValidator`
4. `CombatDomain.Land` — `LandAspectDomainValidator`
5. `CombatDomain.Mine` — `MineAspectDomainValidator`

## Files changed

- `src/ProjectAegis.Sim/Engage/MineAspectDomainValidator.cs` — new
- `src/ProjectAegis.Sim/Engage/EngageContext.cs` — `MineAspectInEnvelope`
- `src/ProjectAegis.Sim/Engage/DomainValidatorRegistry.cs` — register mine validator
- `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs` — `MapDomainDenial` for `MineAspectBlock`
- `src/ProjectAegis.Sim/Policy/FireAbortReason.cs` — `MineAspectBlock`
- `src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs` — `MineAspectBlock`
- `data/glossary/abort_reason_manifest.json` — `MINE_ASPECT_BLOCK`
- `src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs` — codegen constants
- `src/ProjectAegis.Sim.Tests/Engage/CombatDomainValidatorTests.cs` — mine allow/deny/boundary
- `src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs` — registry, resolver, order-log, flag-off

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Mine" -v minimal   # 52/52 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal   # 6/6 PASS
dotnet test ProjectAegis.sln -v minimal   # full sln PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` (production policy unchanged by this story)
- Facility runtime validator (deferred S31-05)

## Unblocks

- **S31-05** — facility combat hot-tick
- **S31-13** — Phase 5 closeout