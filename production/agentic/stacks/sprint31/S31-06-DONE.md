# S31-06 story-done — BDA Hot Path (Bounded)

**Story:** `production/epics/sprint-31-combat-domains-phase5/story-031-06-bda-hot-path.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint31/bda-hot-path`  
**Builds on:** S27-06 `OrderLogBdaProjection` + S30-08 `CatalogDamageHotTickApplier` / `PlatformDamageChangeRecord`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Hit outcomes with `damageLevel` 0–3 drive contact status transitions in BDA projections | `OrderLogBdaProjectionTests` (levels 0–3, escalation) | **PASS** |
| `ContactPictureProjection` / BDA projection reflects damage-level contact status (not kill-only) | `ProjectWithBda` + `BdaContactDamageStates` (`Degraded-L1` / `Degraded-L2`) | **PASS** |
| Projection-only — no sim-kernel contact mutation in default config | `ContactPictureProjection.ProjectWithBda` only; no sensor sim changes | **PASS** |
| Flag-gated test fixture only (`combatDomainsEnabled=true`) | All BDA tests assert `combatDomainsEnabled` gate | **PASS** |
| BDA projection tests PASS (`Combat\|Domain\|Bda` filters) | `OrderLogBdaProjectionTests` + `BdaContactDamageStatesTests` | **PASS** |
| `ReplayGoldenSuiteTests` — 6/6 PASS on default path | unchanged production Baltic pins | **PASS** |
| No full BDA component model / component-level damage | order-log projection + `damageLevel` map only | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | **PASS** |

## QA test-case traceability

| Case | Test / Evidence | Status |
|------|-----------------|--------|
| AC-1 Damage-level contact status projection | `Hit_damage_level_1/2/3_*`, `Escalating_hits_promote_contact_to_lost` | **PASS** |
| AC-1 miss unchanged | `Miss_outcome_leaves_contact_picture_unchanged` | **PASS** |
| AC-1 multiple hits stable order | `Multiple_hits_apply_in_stable_tick_then_sequence_order` | **PASS** |
| AC-1 kill drops contact | `Killed_contact_drops_from_contact_picture`, `Multiple_kills_apply_in_stable_engagement_then_sequence_order` | **PASS** |
| AC-1 boundary damageLevel 0 | `Hit_damage_level_zero_leaves_contact_unchanged` | **PASS** |
| AC-2 default path replay unchanged | `ReplayGoldenSuiteTests` 6/6; golden catalog has no `catalogWithdrawTargets` | **PASS** |

## Key projection behavior

| Input | `damageLevel` / outcome | Contact picture result |
|-------|-------------------------|------------------------|
| `PlatformDamageChange` Hit | 1 | `LifecycleState = Degraded-L1` |
| `PlatformDamageChange` Hit | 2 | `LifecycleState = Degraded-L2` |
| `PlatformDamageChange` Hit | 3 or `NewHpPct <= 0` | Contact dropped (`Lost`) |
| `EngagementOutcome` Kill | — | Contact dropped (`Lost`) |
| `PlatformDamageChange` AmbientTick | 0 | Unchanged (no BDA side effect) |
| Miss / `damageLevel` 0 Hit | — | Unchanged |

Ordering: `SimTick` → `EngagementId` (kill outcomes) → `SequenceId`; escalating hits monotonically promote degraded state.

## Files changed

- `src/ProjectAegis.Delegation/Projection/BdaContactDamageStates.cs` — damage-level → contact status labels
- `src/ProjectAegis.Delegation/Projection/OrderLogBdaProjection.cs` — `ProjectBdaContactChanges` (damage + kill); engagement-ordered merge
- `src/ProjectAegis.Delegation/Projection/ContactPictureProjection.cs` — `ProjectWithBda` uses full BDA change set
- `src/ProjectAegis.Delegation/Decision/PlatformDamageChangeRecord.cs` — optional `DamageLevel` field
- `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` — fingerprint includes `DamageLevel`
- `src/ProjectAegis.Delegation/Sim/CatalogDamageHotTickTracker.cs` — propagate `DamageLevel` to order log
- `src/ProjectAegis.Delegation.Tests/Projection/OrderLogBdaProjectionTests.cs` — damage-level + kill regression tests
- `src/ProjectAegis.Delegation.Tests/Projection/BdaContactDamageStatesTests.cs` — mapping unit tests
- `src/ProjectAegis.Sim.Tests/Catalog/CombatDamageLevelTests.cs` — `CombatDomain_Bda_damage_level_stays_within_hot_path_envelope`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Bda|Damage" -v minimal
# Passed: 104/104

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "Combat|Domain|Bda|Damage" -v minimal
# Passed: 31/31

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Per-project counts

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Bda\|Damage | 104 |
| ProjectAegis.Delegation.Tests | Combat\|Domain\|Bda\|Damage | 31 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol.policy.json` production golden pins
- Full BDA component model / mine-land-facility runtime
- Sim-kernel contact mutation