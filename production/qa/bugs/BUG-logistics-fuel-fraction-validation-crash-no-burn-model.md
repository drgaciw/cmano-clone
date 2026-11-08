# Bug Report

## Summary
**Title**: `ScenarioLogisticsSettings` constructor can crash scenario load over fuel-fraction fields that have no gameplay effect
**ID**: BUG-LOGISTICS-FUEL-FRACTION-VALIDATION-CRASH-NO-BURN-MODEL
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Verified Fixed (fixed in same commit as this report)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (headless TDD loop, qa-r2-05-logistics, round 2)

## Classification
- **Category**: Gameplay (Logistics / scenario content authoring — scenario load reliability)
- **System**: `ProjectAegis.Sim.Scenario.ScenarioLogisticsSettings` (fuel/endurance settings for a scenario policy profile; consumed by `ProjectAegis.Delegation.Logistics.FuelTimelineTracker`, `ProjectAegis.Delegation.Projection.FuelStateProjection`, `ProjectAegis.Delegation.Projection.UnitDetailProjection`)
- **Frequency**: Often (>50%) for any scenario JSON that authors a `logistics` block without enabling the fuel burn model (i.e. omits/zeroes `FuelCapacityKg` or `BurnRateKgPerSecond`) while touching only one of `JokerFuelFraction`/`BingoFuelFraction` — a plausible authoring pattern (copying a `logistics` block from a burn-model scenario as a starting point, or staging a fraction override ahead of enabling the burn model later)
- **Regression**: No — this validation has been unconditional since the class was written; not a regression from a prior working state, and distinct from the round-1 `FuelLedger.AdvanceTick` negative-delta overfill fix (that was a runtime per-tick math clamp; this is a constructor-time validation defect that rejects otherwise-harmless scenario content before any tick ever runs).

## Environment
- **Build**: branch `qa-r2-05-logistics`, based on main `e2e1342`
- **Platform**: headless .NET 8 test layer (no Unity Editor in this sandbox); logic is engine-agnostic C# under `src/ProjectAegis.Sim`
- **Scene/Level**: N/A (scenario-policy load path, exercised via `ScenarioPolicyJsonLoader.LoadFromFile` / `ToProfile`)
- **Game State**: Any scenario JSON with a `logistics` block that leaves the fuel burn model disabled (`FuelCapacityKg`/`BurnRateKgPerSecond` unset or zero) but sets `BingoFuelFraction` above the default `JokerFuelFraction` (0.25), or vice versa in a way that violates `bingo <= joker`

## Reproduction Steps
**Preconditions**: None beyond a scenario logistics settings construction path (JSON loader or direct call).

1. Construct `new ScenarioLogisticsSettings(jokerSimSeconds: 300, bingoSimSeconds: 600, bingoFuelFraction: 0.30)` — i.e. leave `fuelCapacityKg`/`burnRateKgPerSecond` at their default `0` (burn model disabled) and only override `bingoFuelFraction` to a value above the default `jokerFuelFraction` (0.25).
2. Observe the constructor throws `ArgumentOutOfRangeException` instead of returning a usable settings object.

**Expected Result**: Since `UsesFuelBurnModel` is `false` in this configuration, `JokerFuelFraction`/`BingoFuelFraction` are never read by any consumer (`FuelStateProjection.ResolveState` and `FuelLedger.ResolveBand` only consult fractions when the burn model is active; `RemainingFuelFraction` and `RemainingFuelKg` are likewise gated behind `UsesFuelBurnModel` at every call site). Construction should succeed — inert fields should not be able to fail scenario load.

**Actual Result (before fix)**: `ArgumentOutOfRangeException: Fuel fractions must be in (0,1] with bingo <= joker. (Parameter 'jokerFuelFraction')` — an unconditional validation check ran regardless of whether the burn model was active, so a scenario/profile with an entirely disabled fuel model would still fail to construct (and, via `ScenarioPolicyJsonLoader.ParseLogistics`, fail to load) purely because of unused fraction fields.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Scenario/ScenarioLogisticsSettings.cs` — constructor (fixed: fraction range/ordering check now gated on `fuelCapacityKg > 0 && burnRateKgPerSecond > 0`, mirroring the existing `UsesFuelBurnModel` property)
  - `src/ProjectAegis.Sim.Tests/Scenario/ScenarioLogisticsSettingsTests.cs` — new regression tests `Constructor_ignores_out_of_order_fuel_fractions_when_burn_model_is_disabled` and `Constructor_still_enforces_fraction_ordering_when_burn_model_is_active`
- **Related systems**: `ProjectAegis.Sim.Scenario.ScenarioPolicyJsonLoader.ParseLogistics` is the sole production caller that feeds arbitrary content-authored values into this constructor (`src/ProjectAegis.Sim/Scenario/ScenarioPolicyJsonLoader.cs:263-273`); it passes `ScenarioLogisticsJsonDto`'s `JokerFuelFraction`/`BingoFuelFraction` fields straight through, and those DTO fields carry compile-time defaults (0.25 / 0.10) matching `ScenarioLogisticsSettings.Default`, so any scenario JSON that overrides only one of the two fields risks colliding with the other field's default regardless of whether the burn model is even configured. `ScenarioLogisticsSettings.Default` itself and all pre-existing test call sites happen to use consistent (or capacity/burn-active-and-consistent) values, so the defect was previously unobserved.
- **Possible root cause**: The fraction range/ordering guard was written unconditionally, without considering that `JokerFuelFraction`/`BingoFuelFraction` are only meaningful — and only read anywhere in the codebase — when `UsesFuelBurnModel` (`FuelCapacityKg > 0 && BurnRateKgPerSecond > 0`) is true.

## Evidence
- **Logs**: xUnit failure before fix:
  ```
  Failed ProjectAegis.Sim.Tests.Scenario.ScenarioLogisticsSettingsTests.Constructor_ignores_out_of_order_fuel_fractions_when_burn_model_is_disabled
  Error Message:
   System.ArgumentOutOfRangeException : Fuel fractions must be in (0,1] with bingo <= joker. (Parameter 'jokerFuelFraction')
  Stack Trace:
     at ProjectAegis.Sim.Scenario.ScenarioLogisticsSettings..ctor(...) in .../ScenarioLogisticsSettings.cs:line 34
  ```
- **After fix**: both new tests pass (`Passed! - Failed: 0, Passed: 2, Skipped: 0, Total: 2`), and the full `ProjectAegis.Sim.Tests` suite passes 287/287 (285 baseline + 2 new), with zero regressions in `ProjectAegis.Delegation.Tests` (253/253), `ProjectAegis.Delegation.UnityAdapter.Tests` (261/261), `ProjectAegis.Data.Tests` (477/477), and `ProjectAegis.Data.Excel.Tests` (5/5).
- **Behavior-preservation argument**: for every currently-reachable construction where the burn model is active (`fuelCapacityKg > 0 && burnRateKgPerSecond > 0`), the new code runs the exact same check as before (only wrapped in an additional `&&` condition that is already true on that path), so no currently-passing burn-model-active construction can change outcome. The change only widens acceptance for the burn-model-inactive case, where the validated fields have no observable effect on any downstream consumer.

## Related Issues
- Distinct from `BUG-fuelledger-negative-delta-overfill.md` (round 1): that bug was a runtime per-tick clamp defect in `FuelLedger.AdvanceTick`; this bug is a constructor-time validation defect in `ScenarioLogisticsSettings` that can block scenario load entirely, before any simulation tick runs.

## Notes
- GitNexus (`.gitnexus/run.cjs`) is not reachable from this isolated worktree, so impact analysis for the `ScenarioLogisticsSettings` constructor was performed manually via `grep` across `src/` rather than via the GitNexus MCP/CLI tooling mandated by the repo's `CLAUDE.md`. Manual trace of all construction call sites: `ScenarioLogisticsSettings.Default` (static field, capacity=0/burn=0, default fractions 0.25/0.10 — unaffected), `ScenarioPolicyJsonLoader.ParseLogistics` (sole production caller — this is exactly the reachable path for the bug), and five existing test call sites (`FuelTimelineTrackerTests.cs`, `UnitDetailProjectionTests.cs`, `FuelStateProjectionTests.cs` x2) — all either burn-model-inactive-with-consistent-defaults or burn-model-active-with-consistent-fractions, so none are affected by the fix. **Risk: LOW** — the fix is provably behavior-preserving for every currently-passing construction (see Evidence above) and only relaxes a previously-crashing, functionally-inert case.
- This worktree was found to have uncommitted changes from a concurrent QA loop touching `src/ProjectAegis.Sim/Engage/CombatDomainValidator.cs` and `src/ProjectAegis.Sim.Tests/Engage/DomainValidatorRegistryTests.cs` (unrelated to this bug). Those files were deliberately left untouched and unstaged; only the two files listed under Technical Context were staged/committed for this bug.

## Closure Record
**Closed**: 2026-07-06
**Resolution**: Fixed — `ScenarioLogisticsSettings`'s constructor now only validates `JokerFuelFraction`/`BingoFuelFraction` range and ordering when the fuel burn model is actually active (`fuelCapacityKg > 0 && burnRateKgPerSecond > 0`), matching the existing `UsesFuelBurnModel` gate used by every consumer of those fields.
**Fix commit / PR**: local commit on `qa-r2-05-logistics` (this loop) — see branch history for hash.
**Verified by**: gameplay-qa-agent (TDD red/green cycle: confirmed new test fails with the `ArgumentOutOfRangeException` before the fix; applied minimal fix; confirmed both new tests pass; ran full `ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests`, `ProjectAegis.Data.Tests`, `ProjectAegis.Data.Excel.Tests` with zero regressions)
**Closed by**: gameplay-qa-agent
**Regression test**: `src/ProjectAegis.Sim.Tests/Scenario/ScenarioLogisticsSettingsTests.cs::Constructor_ignores_out_of_order_fuel_fractions_when_burn_model_is_disabled` (plus companion `Constructor_still_enforces_fraction_ordering_when_burn_model_is_active`)
**Status**: Closed
