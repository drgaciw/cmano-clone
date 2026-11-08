# S30-08 story-done — Hot-tick Hit → HP Ledger Extensions

**Story:** `production/epics/sprint-30-combat-domains-phase4/story-030-08-hot-tick-hits.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/hot-tick-hits`  
**Builds on:** S29-09 `CatalogDamageHotTickApplier` + S30-05 land validator

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Engagement `Hit` outcomes apply HP delta via `DeterministicDamageApplyBatch.Sort` → `PlatformHpLedger` | `CatalogDamageHotTickApplier.ApplySortedOutcomes`; `DeterministicDamageApplyBatchTests` | **PASS** |
| `damageLevel = clamp(0, 3, floor(hitSeverity * platform.resilience))` on Hit path | `CombatDamageLevel` + `CombatDamageLevelTests` | **PASS** |
| Damage from gate-approved catalog snapshot (no hot-path SQLite) | `ICatalogReader.TryGetPlatformDamage`; `CatalogPlatformDamage.Resilience` default | **PASS** |
| `PlatformDamageChange` order-log rows on HP transitions | `CatalogDamageHotTickTracker`; `Session_hit_outcome_logs_platform_damage_change_with_hit_reason` | **PASS** |
| Sim tests PASS (`Combat\|Domain\|Damage`) | **88/88** PASS | **PASS** |
| `/replay-verify` on isolated hot-tick hit fixture | `BalticReplayHarnessHotTickHitTests` determinism + fingerprint | **PASS** |
| `ReplayGoldenSuiteTests` — 6/6 default path | unchanged production Baltic pins | **PASS** |
| No full BDA component model | ledger HP% + withdraw refresh only | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | **PASS** |

## QA test-case traceability

| Case | Test / Evidence | Status |
|------|-----------------|--------|
| AC-1 Hit outcome → HP ledger apply | `HotTick_Hit_and_kill_outcomes_apply_in_sorted_order`, `HotTick_Hit_damage_level_bounded_0_to_3` | **PASS** |
| AC-1 multiple hits same tick | `DeterministicDamageApplyBatchTests.HotTick_sorted_outcomes_drive_catalog_damage_apply_order` | **PASS** |
| AC-1 Kill vs Hit precedence | `HotTick_Kill_outcome_zeroes_hp_after_prior_hit` | **PASS** |
| AC-1 zero resilience | `HotTick_Zero_resilience_hit_produces_no_hp_change` | **PASS** |
| AC-1 missing damage row | `HotTick_Missing_damage_row_skips_hit_apply` | **PASS** |
| AC-2 deterministic apply order | `DeterministicDamageApplyBatchTests`; `HotTick_Repeat_apply_is_deterministic` | **PASS** |
| AC-3 default path replay unchanged | `ReplayGoldenSuiteTests` 6/6; fixture not in golden catalog | **PASS** |

## Files changed

- `src/ProjectAegis.Sim/Catalog/CombatDamageLevel.cs` — GDD damageLevel 0–3 formula + HP% mapping
- `src/ProjectAegis.Sim/Catalog/CatalogDamageHotTickApplier.cs` — Hit path uses damageLevel; `OutcomeApply.HitSeverity`; `DamageChange.DamageLevel`
- `src/ProjectAegis.Data/Catalog/CatalogPlatformDamage.cs` — optional `Resilience` (default 1.0, GDD 0.5–2 clamp at apply)
- `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` — pass `HitSeverity` on Hit outcomes
- `data/scenarios/baltic-patrol-combat-domains-hot-tick-damage.policy.json` — `pkKill: 0` for Hit path isolation
- `src/ProjectAegis.Sim.Tests/Catalog/CombatDamageLevelTests.cs` — formula + HP delta tests
- `src/ProjectAegis.Sim.Tests/Catalog/CatalogDamageHotTickApplierTests.cs` — Hit/damageLevel edge cases
- `src/ProjectAegis.Delegation.Tests/Sim/CatalogDamageHotTickEngageTests.cs` — session Hit → `PlatformDamageChange`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessHotTickHitTests.cs` — isolated replay-verify

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|HotTick" -v minimal
# Passed: 88/88

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "CatalogDamageHotTick" -v minimal
# Passed: 3/3

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests|BalticReplayHarnessHotTickHitTests" -v minimal
# Passed: 10/10 (ReplayGolden 6/6 + hot-tick hit 4/4)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Damage\|HotTick | 88 |
| ProjectAegis.Delegation.Tests | CatalogDamageHotTick | 3 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests + BalticReplayHarnessHotTickHitTests | 10 |

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` production golden pins
- Full BDA component model / mine-land-facility runtime
- Hot-path SQLite reads