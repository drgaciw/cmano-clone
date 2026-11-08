# S25-13 story-done evidence — damage-sim-consumer

**Branch:** `stack/sprint25/damage-sim-consumer`  
**Story:** `production/sprints/sprint-25-phase-b-damage-assurance.md` §S25-13  
**Status:** Complete  
**Trunk evidence:** `main` @ `c3ae349` (S25-01..12 merged)  
**Completed:** 2026-06-18

## Deliverables

- `PhaseBCatalogDamageReadinessStub`: consumes `ICatalogReader.TryGetPlatformDamage(platformId)`; evaluates withdraw/readiness trials (additive when TryGet misses)
- `InMemoryCatalogReader`: optional Phase B damage rows for test fixtures (Baltic fixture unchanged)
- `PhaseBDamageCatalogConsumerTests.cs`: legacy unchanged, damage delta, boundary threshold, unreferenced platform, critical flags, sorted reads
- **ZERO** touch `DelegationBridge.cs`
- **No runtime damage application / BDA** — stub API only; not wired into hot tick path

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "PhaseB|Damage|Readiness|Withdraw" -v minimal
# Passed: 19

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6 — replay hashes unchanged (Baltic catalog has no Phase B damage rows)

dotnet test ProjectAegis.sln -v minimal
# Passed: 653/653

git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| `ICatalogReader.TryGetPlatformDamage` consumed by sim readiness/withdraw smoke stub | `PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness` | **PASS** |
| Committed damage change affects readiness/withdraw trial outcome (stub only) | `PhaseB_Committed_platform_damage_changes_withdraw_trial_outcome` | **PASS** |
| Fixtures without damage data → identical trial results (additive-only) | Baltic fixture + ReplayGolden 6/6 | **PASS** |
| Sim tests PASS (`PhaseB\|Damage\|Readiness\|Withdraw` filter) | 19/19 PASS | **PASS** |
| ReplayGoldenSuiteTests 6/6 PASS | 6/6 PASS | **PASS** |
| Story-done evidence | This file | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs `main` | **PASS** |
| Test floor ≥592 | **653/653** PASS | **PASS** |

## Per-project counts (full sln)

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 105 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 100 |
| ProjectAegis.Delegation.Tests | 177 |
| ProjectAegis.Data.Excel.Tests | 5 |
| ProjectAegis.Data.Tests | 245 |
| **Total** | **653** |

## Verdict

**COMPLETE** — All S25-13 acceptance criteria satisfied; replay 6/6; no golden drift; stub-only scope lock honored.