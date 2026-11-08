# S32-09 story-done — BDA Contact Lifecycle Sim Hook

**Story:** `production/epics/sprint-32-combat-domains-phase6/story-032-09-bda-lifecycle-hook.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/bda-lifecycle-hook`  
**Builds on:** S31-06 `OrderLogBdaProjection` + S30-08 `CatalogDamageHotTickApplier`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Contact FSM promotes to `Lost` when `damageLevel ≥ 3` on flag-on isolated fixture | `BdaContactLifecycleHotTickApplierTests`; `BalticReplayHarnessBdaLifecycleTests` | **PASS** |
| Sim-kernel contact state consistent with BDA projection output | `Bda_lifecycle_fixture_promotes_contact_to_lost_consistent_with_projection` | **PASS** |
| Flag-gated (`combatDomainsEnabled=true`) — no contact mutation on default path | `Bda_lifecycle_disabled_when_combat_domains_flag_off`; ReplayGolden 6/6 | **PASS** |
| BDA projection tests PASS (`Combat\|Domain\|Bda` filters) | Sim.Tests 126/126 | **PASS** |
| `ReplayGoldenSuiteTests` — 6/6 PASS on default path | unchanged production Baltic pins | **PASS** |
| `/replay-verify` PASS on isolated fixture | `Bda_lifecycle_fixture_replay_is_deterministic` | **PASS** |
| No full BDA component model; no mine-laying / component-level damage | order-log + sim-kernel hook only | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | **PASS** |

## QA test-case traceability

| Case | Test / Evidence | Status |
|------|-----------------|--------|
| AC-1 Contact Lost at damageLevel ≥ 3 | `ShouldPromoteToLost_matches_bda_projection_rules`; harness fingerprint `Lost` | **PASS** |
| AC-1 damageLevel 2 degraded not Lost | `ApplySortedTargets_skips_damage_level_two_without_lost_transition` | **PASS** |
| AC-1 boundary value 3 | `ShouldPromoteToLost` level-3 Hit case | **PASS** |
| AC-1 multiple hits stable order | `ResolveSortedLostTargets_orders_platform_ids_deterministically` | **PASS** |
| AC-2 default path replay unchanged | ReplayGolden 6/6; `BalticCombatDomainsPolicyTests` world hash `17144800277401907079` | **PASS** |

## Bounded hot-tick design

- `BdaContactLifecycleHotTickApplier` — mirrors `OrderLogBdaProjection` Lost rules (`damageLevel ≥ 3`, `NewHpPct ≤ 0`, Kill)
- `BdaContactLifecycleRegistry` — per-tick pending Lost targets (kill-transition pattern)
- `PdDetectionContactSimulator.ApplyTargetBdaLost` — sim-kernel Lost without engage kill registry; blocks re-detection via `_bdaLostTargets`
- Wired post-damage in `SimulationSession.ApplyCatalogDamageHotTick`; drained in `BalticReplayHarness` after `bridge.Tick`
- Isolated fixture: `baltic-patrol-bda-lifecycle` (`hostile-1` catalogWithdraw @ 30% HP, resilience 2.0 → Hit drives `NewHpPct ≤ 0`)

## Files changed

- `src/ProjectAegis.Sim/Catalog/BdaContactLifecycleHotTickApplier.cs` — flag-gated applier + sim transition apply
- `src/ProjectAegis.Sim/Engage/BdaContactLifecycleRegistry.cs` — pending Lost target registry
- `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs` — `ApplyTargetBdaLost` + `_bdaLostTargets` re-detect block
- `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` — registry + post-damage mark Lost
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` — drain registry; `Result.DecisionLog`
- `data/scenarios/baltic-patrol-bda-lifecycle.policy.json` — isolated flag-on fixture
- `src/ProjectAegis.Sim.Tests/Catalog/BdaContactLifecycleHotTickApplierTests.cs` — 8 unit tests
- `src/ProjectAegis.Sim.Tests/Sensors/PdContactBdaLifecycleTests.cs` — 2 sensor tests
- `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs` — fixture loader test
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessBdaLifecycleTests.cs` — 4 replay tests

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Bda|Damage" -v minimal
# Passed: 126/126

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests|BalticReplayHarnessBdaLifecycleTests|BalticCombatDomainsPolicyTests" -v minimal
# Passed: 18/18 (ReplayGolden 6/6, BDA lifecycle 4/4, CombatDomains 8/8)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Bda\|Damage | 126 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |
| ProjectAegis.Delegation.UnityAdapter.Tests | BalticReplayHarnessBdaLifecycleTests | 4 |
| ProjectAegis.Delegation.UnityAdapter.Tests | BalticCombatDomainsPolicyTests | 8 |

## Fixture policy id

`baltic-patrol-bda-lifecycle`

## Not touched (by design)

- `DelegationBridge.cs`
- `ReplayGoldenRegressionCatalog` (BDA lifecycle fixture excluded)
- `baltic-patrol.policy.json` production golden pins
- Full BDA component model