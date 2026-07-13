# S28-08 story-done evidence — Damage Sim Consumer Wire (Beyond Stub)

**Story:** `production/epics/sprint-28-combat-domains-phase2/story-028-08-damage-consumer-wire.md`  
**Status:** Complete  
**Trunk evidence:** `main` @ `12f1eb5`  
**Completed:** 2026-06-18

## Deliverables

- `PhaseBCatalogDamageReadinessStub` extended with `TryResolveScenarioTrial` (MaxHp / WithdrawThresholdPct / CriticalFlags → scenario DTO)
- `CatalogDamageWithdrawEngageGate` — bounded engage gate consuming catalog-resolved withdraw trials
- `EngageContext.CatalogDamageWithdrawBlocked` + `EngagementAbortReason.DamageWithdrawRecommended` (`DAMAGE_WITHDRAW_RECOMMENDED`)
- `WithdrawReadinessTrialResolver` refactored to use stub scenario-trial helper
- `SimulationSession.PrimeEngageWorld` wires `CatalogWithdrawTrials` into engage context (no hot-tick damage apply)
- `MvpEngagementResolver` aborts on catalog damage withdraw recommendation
- `data/scenarios/baltic-patrol-damage-withdraw.policy.json` — test-only fixture (not in ReplayGolden 6/6)
- Tests: `MvpEngagementDamageWithdrawTests`, `CatalogDamageWithdrawEngageGateTests`, extended `PhaseBDamageCatalogConsumerTests` (Baltic+seeded damage), `CatalogDamageWithdrawEngageTests`, `BalticReplayHarnessDamageWithdrawTests`
- **ZERO** touch `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness" -v minimal
# Passed: 57/57

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6 — Baltic production fixtures unchanged (no catalog damage rows; no catalogWithdraw on golden policies)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Stub extended with catalog damage columns | `TryResolveScenarioTrial` + existing `EvaluateWithdrawReadiness` | **PASS** |
| Readiness/withdraw evaluation consumes catalog damage | `WithdrawReadinessTrialResolver` + `ReadinessPolicyEvaluator` + engage gate | **PASS** |
| Sim tests PASS (`Combat\|Domain\|Damage\|Readiness`) | **57/57** PASS | **PASS** |
| ReplayGoldenSuiteTests 6/6 | 6/6 PASS | **PASS** |
| `/replay-verify` on merge | ReplayGolden 6/6; Baltic `combatDomainsEnabled=false` | **PASS** |
| No hot-tick world-state damage apply | Trial resolution + engage abort only; no BDA batch | **PASS** |
| `combatDomainsEnabled=false` on Baltic production fixtures | Golden policies unchanged; damage fixture test-only | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Damage\|Readiness | 57 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |
| ProjectAegis.Delegation.Tests | CatalogDamageWithdraw | 1 |

## Verdict

**COMPLETE** — Phase B damage catalog wired through policy resolver into bounded engage gate; replay 6/6; no Baltic golden drift; stub-only scope lock honored.