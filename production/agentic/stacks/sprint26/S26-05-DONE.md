# S26-05 story-done evidence — damage sim consumer wire (bounded)

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-05  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- `WithdrawReadinessTrialResolver` — applies `PhaseBCatalogDamageReadinessStub` to `catalogWithdraw` policy targets (S24-06 detection pattern)
- `ReadinessPolicyEvaluator` — merges scenario `unitReadiness` with catalog withdraw trials
- `ScenarioCatalogWithdrawTarget` / `ScenarioWithdrawReadinessTrial` — policy DTOs
- `ScenarioPolicyProfile` + `ScenarioPolicyJsonLoader` — `catalogWithdraw` JSON parsing
- `BalticReplayHarness` — wires resolver at session setup; binds `SimulationSession.CatalogWithdrawTrials`
- Tests: `WithdrawReadinessTrialResolverTests.cs`, extended `PhaseBDamageCatalogConsumerTests.cs`
- **ZERO touch** `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Damage|PhaseB|Readiness|Withdraw" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Sim filter: 24/24 PASS; ReplayGolden: 6/6 PASS
```

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| Wire stub to policy/readiness path | `WithdrawReadinessTrialResolver` + `ReadinessPolicyEvaluator` + harness bind | **PASS** |
| No BDA / no hot-tick damage apply | Stub-only evaluation; no engage-path mutation | **PASS** |
| `combatDomainsEnabled` stays false | Not introduced (S26-09 scope) | **PASS** |
| Sim tests PASS | Damage/PhaseB/Readiness/Withdraw filter **24/24** | **PASS** |
| ReplayGolden 6/6 | Baltic fixtures additive-only → hashes unchanged | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs `main` | **PASS** |

## Verdict

**COMPLETE** — bounded catalog withdraw readiness wired; replay determinism preserved.