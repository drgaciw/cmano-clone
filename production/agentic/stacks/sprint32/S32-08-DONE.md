# S32-08 story-done — Mine Transit Hazard Hot-Tick (Bounded)

**Story:** `production/epics/sprint-32-combat-domains-phase6/story-032-08-mine-transit-hazard.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/mine-transit-hazard`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Scenario policy supports optional `mineHazard` zone + seeded placement | `Mine_transit_hazard_policy_json_round_trips_zone_mines_and_transit`; `Loads_baltic_patrol_mine_transit_hazard_fixture` | COVERED |
| Isolated fixture `baltic-patrol-mine-transit-hazard` exercises transit hazard hot-tick | `BalticReplayHarnessMineTransitHazardTests` (4 tests) | COVERED |
| Hazard evaluation deterministic across replays — stable order log | `Mine_transit_hazard_same_seed_produces_identical_outcome_set`; `Mine_transit_hazard_fixture_replay_is_deterministic` | COVERED |
| No mine-laying / mine-clearing missions; no full danger-area map layer | Policy JSON only; no mission types or map layer code | COVERED |
| Sim tests PASS (`Combat\|Domain\|Mine` filters) | 77/77 PASS | COVERED |
| `/replay-verify` PASS on isolated fixture | `Mine_transit_hazard_fixture_replay_is_deterministic` + fingerprint SHA256 match | COVERED |
| Fixture **not** in ReplayGolden 6/6; production Baltic hash unchanged | `Policy_is_not_in_replay_golden_regression_catalog`; `BalticCombatDomainsPolicyTests` pin `17144800277401907079` | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 Transit hazard hot-tick | `Mine_transit_hazard_applies_deterministic_hp_delta_inside_zone`; harness fingerprint contains `MINE_TRANSIT_HAZARD` | COVERED |
| AC-1 Platform skirts zone boundary | `Mine_transit_hazard_skirts_zone_boundary_without_hp_change` | COVERED |
| AC-1 Multiple platforms same tick | `Mine_transit_hazard_multiple_platforms_evaluated_in_stable_platform_order` | COVERED |
| AC-1 Empty hazard zone | `Mine_transit_hazard_empty_zone_produces_no_changes` | COVERED |
| AC-1 Flag-off path | `Mine_transit_hazard_disabled_when_combat_domains_flag_off` | COVERED |
| AC-2 Golden catalog isolation | ReplayGolden 6/6; fixture excluded from catalog | COVERED |

## Bounded hot-tick design

- Policy `mineHazard`: zone bounds, `triggerRadiusMeters`, seeded `mines[]`, authored `transit[]` ranges per tick
- `MineTransitHazardHotTickApplier` — deterministic `SeededRng` draws (`RngDomain.MineHazard`) sorted by `(platformId, mineId)`
- HP apply via existing `PlatformHpLedger` + `PlatformDamageChange` order log (`MINE_TRANSIT_HAZARD` reason)
- Wired through `CatalogDamageHotTickTracker.ApplyTick` (no `DelegationBridge` changes)

## Files changed

- `src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs` — `mineHazard` JSON DTOs
- `src/ProjectAegis.Sim/Scenario/ScenarioMineHazard.cs` — profile models
- `src/ProjectAegis.Sim/Scenario/ScenarioPolicyProfile.cs` — `MineHazard` property
- `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs` — parse/bind `mineHazard`
- `src/ProjectAegis.Sim/Core/RngDomain.cs` — `MineHazard` domain
- `src/ProjectAegis.Sim/Catalog/MineTransitHazardEntityId.cs` — stable RNG entity id
- `src/ProjectAegis.Sim/Catalog/MineTransitHazardHotTickApplier.cs` — bounded transit hazard evaluation
- `src/ProjectAegis.Sim/Catalog/CatalogDamageHotTickApplier.cs` — `MINE_TRANSIT_HAZARD` reason code
- `src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs` — mine hazard tick integration
- `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` — pass global seed to tracker
- `data/scenarios/baltic-patrol-mine-transit-hazard.policy.json` — isolated flag-on fixture
- `src/ProjectAegis.Sim.Tests/Catalog/MineTransitHazardHotTickApplierTests.cs` — 7 unit tests
- `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs` — fixture loader test
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessMineTransitHazardTests.cs` — 4 replay tests

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Mine" -v minimal   # 77/77 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal   # 6/6 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "BalticCombatDomainsPolicyTests" -v minimal   # 8/8 PASS (world hash 17144800277401907079)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "BalticReplayHarnessMineTransitHazardTests" -v minimal   # 4/4 PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Per-project counts

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Mine | 77 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |
| ProjectAegis.Delegation.UnityAdapter.Tests | BalticCombatDomainsPolicyTests | 8 |
| ProjectAegis.Delegation.UnityAdapter.Tests | BalticReplayHarnessMineTransitHazardTests | 4 |

## Not touched (by design)

- `DelegationBridge.cs`
- `ReplayGoldenRegressionCatalog` (mine-transit fixture excluded)
- `baltic-patrol.policy.json` production golden pins
- Mine-laying / mine-clearing mission types
- Full danger-area map layer

## Unblocks

- **S32-09** — BDA contact lifecycle sim hook
- Phase 6 combat-domain closeout stories