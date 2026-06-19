# S31-05 story-done — Facility Combat Hot-Tick

**Story:** `production/epics/sprint-31-combat-domains-phase5/story-031-05-facility-hot-tick.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/facility-hot-tick`  
**Builds on:** S28-09 `OrderLogFacilityDamageProjection` + S30-08 `CatalogDamageHotTickApplier`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Facility engagement `Hit` outcomes apply HP delta via `DeterministicDamageApplyBatch` → `PlatformHpLedger` | `CatalogFacilityDamageHotTickApplierTests.Facility_Domain_HotTick_hit_applies_sorted_hp_delta_to_facility_ledger` | **PASS** |
| `damageLevel` bounded 0–3 per GDD on facility hot-tick path | `Facility_Domain_Damage_hot_tick_hit_damage_level_bounded_0_to_3` | **PASS** |
| Extends S28-09 projection stub — order-log + picture projection consistent with HP state | `OrderLogFacilityDamageProjectionHotTickTests` (5 tests) | **PASS** |
| Damage from gate-approved catalog snapshot (no hot-path SQLite) | `ICatalogReader.TryGetPlatformDamage`; `InMemoryCatalogReader` fixture | **PASS** |
| Sim tests PASS (`Combat\|Domain\|Damage\|Facility`) | **104/104** PASS | **PASS** |
| `/replay-verify` on isolated facility hot-tick fixture | `BalticReplayHarnessFacilityHotTickTests` determinism + fingerprint | **PASS** |
| `ReplayGoldenSuiteTests` — 6/6 default path | unchanged production Baltic pins | **PASS** |
| No full facility component model | ledger HP% + projection capacity only | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | **PASS** |

## QA test-case traceability

| Case | Test / Evidence | Status |
|------|-----------------|--------|
| AC-1 Facility hit → HP ledger apply | `Facility_Domain_HotTick_hit_applies_sorted_hp_delta_to_facility_ledger` | **PASS** |
| AC-1 multiple hits same tick | sorted apply order via `DeterministicDamageApplyBatch` | **PASS** |
| AC-1 Kill vs Hit precedence | `Facility_Domain_Damage_kill_outcome_zeroes_facility_hp_after_prior_hit` | **PASS** |
| AC-1 zero resilience | `Facility_Domain_Damage_zero_resilience_hit_produces_no_hp_change` | **PASS** |
| AC-1 missing damage row | `Facility_Domain_Damage_missing_damage_row_skips_facility_hit_apply` | **PASS** |
| AC-1 picture projection consistent with HP | `Facility_Domain_Damage_hp_ledger_hit_projects_damaged_capacity` | **PASS** |
| AC-2 production Baltic regression | `ReplayGoldenSuiteTests` 6/6; fixture not in golden catalog | **PASS** |

## Files changed

- `src/ProjectAegis.Sim/Catalog/FacilityHpCapacity.cs` — HP% → facility capacity state mapping
- `src/ProjectAegis.Sim/Catalog/CatalogDamageHotTickApplier.cs` — `ApplySortedFacilityOutcomes` for `CombatDomain.Facility`
- `src/ProjectAegis.Delegation/Projection/OrderLogFacilityDamageProjection.cs` — HP-ledger projection with S28-09 outcome fallback
- `data/scenarios/baltic-patrol-combat-domains-facility-hot-tick.policy.json` — isolated flag-on fixture (`combatDomain: Facility`, `pkKill: 0`)
- `src/ProjectAegis.Sim.Tests/Catalog/CatalogFacilityDamageHotTickApplierTests.cs` — facility hot-tick edge cases
- `src/ProjectAegis.Delegation.Tests/Projection/OrderLogFacilityDamageProjectionHotTickTests.cs` — HP-consistent projection tests
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessFacilityHotTickTests.cs` — isolated replay-verify

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Facility" -v minimal
# Passed: 104/104

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "Facility" -v minimal
# Passed: 12/12

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests|BalticReplayHarnessFacilityHotTickTests" -v minimal
# Passed: 10/10 (ReplayGolden 6/6 + facility hot-tick 4/4)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Damage\|Facility | 104 |
| ProjectAegis.Delegation.Tests | Facility | 12 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests + BalticReplayHarnessFacilityHotTickTests | 10 |

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` production golden pins
- Full facility component model / mine-land-facility runtime
- Hot-path SQLite reads