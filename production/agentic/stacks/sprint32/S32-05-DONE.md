# S32-05 story-done — ECCM Scenario Factor (Bounded Phase 2)

**Story:** `production/epics/sprint-32-combat-domains-phase6/story-032-05-eccm-scenario-factor.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Branch:** `stack/sprint32/eccm-scenario-factor`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Optional `eccmFactor` on `ScenarioDetectionTrial` + policy JSON binding | `Eccm_factor_defaults_to_neutral_when_omitted_from_policy_json`; `Baltic_patrol_jammed_fixture_loads_authored_eccm_factor` | COVERED |
| `baltic-patrol-jammed` isolated fixture demonstrates ECCM factor effect | `Eccm_factor_scales_pd_under_partial_jam`; fixture `eccmFactor: 0.75` authored | COVERED |
| Default production Baltic unchanged — ReplayGolden 6/6; world hash `17144800277401907079` | `ReplayGoldenSuiteTests` 6/6; `BalticCombatDomainsPolicyTests` pin | COVERED |
| Sim tests PASS (`Combat\|Eccm\|Domain` filters) | 75/75 PASS | COVERED |
| `/replay-verify` PASS on isolated jammed fixture | `Eccm_jammed_fixture_replay_is_deterministic`; `Detection_world_hash_stable_for_jammed_run` | COVERED |
| No catalog onboard ECCM flags; no hot-path SQLite | `ScenarioCatalogDetectionTarget` unchanged; catalog resolver uses default `EccmFactor=1.0` | COVERED |
| ZERO touch `DelegationBridge.cs` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 ECCM factor on detection trial | `EccmScenarioFactorTests` (neutral default, scaling, boundary, determinism) | COVERED |
| AC-2 Production Baltic regression | ReplayGolden 6/6; world hash `17144800277401907079`; jammed fixture ∉ golden catalog | COVERED |

## GDD formula wired

```
Pd = clamp01(basePd * envMask * eccmFactor * (1 - jamStrength))
```

`DeterministicDetectionLoop` passes `trial.EccmFactor` into `DetectionProbability.ComputePd`.

## Files changed

- `src/ProjectAegis.Sim/Scenario/ScenarioDetectionTrial.cs` — optional `EccmFactor` (default `1.0`)
- `src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonDto.cs` — `eccmFactor` JSON field
- `src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs` — parse/bind `eccmFactor`
- `src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs` — wire ECCM into Pd loop
- `src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs` — named `RequiresActiveRadar` after new field
- `data/scenarios/baltic-patrol-jammed.policy.json` — isolated fixture `eccmFactor: 0.75`
- `src/ProjectAegis.Sim.Tests/Sensors/EccmScenarioFactorTests.cs` — new ECCM test suite
- `src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs` — formula assertion
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessJamTests.cs` — golden exclusion + replay determinism

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Eccm|Domain" -v minimal   # 75/75 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal   # 6/6 PASS
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "BalticCombatDomainsPolicyTests" -v minimal   # 8/8 PASS (world hash 17144800277401907079)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "BalticReplayHarnessJamTests" -v minimal   # 4/4 PASS
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Not touched (by design)

- `DelegationBridge.cs`
- `ScenarioCatalogDetectionTarget` (no catalog onboard ECCM)
- `baltic-patrol.policy.json` production policy
- `ReplayGoldenRegressionCatalog` (jammed fixture excluded)

## Unblocks

- **S32-08** — mine transit hazard
- **S32-09** — BDA contact lifecycle sim hook
- Phase 6 combat-domain closeout stories