# Bug Report

## Summary
**Title**: `FuelLedger.AdvanceTick` can report fuel above tank capacity on a negative-delta tick
**ID**: BUG-FUELLEDGER-NEGATIVE-DELTA-OVERFILL
**Severity**: S3-Minor
**Priority**: P3-Backlog
**Status**: Verified Fixed (fixed in same commit as this report)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (headless TDD loop, qa-loop-05-logistics)

## Classification
- **Category**: Gameplay (Logistics / simulation invariant)
- **System**: `ProjectAegis.Sim.Logistics.FuelLedger` (per-unit fuel burn ledger, logistics GDD F' = F - burn * Δt)
- **Frequency**: Rare (<10%) — only reachable if a `deltaSeconds` value less than zero ever reaches `AdvanceTick`. No current production caller does this (see Technical Context), so as of this commit the defect is latent/defensive rather than currently player-visible.
- **Regression**: No — this is a previously-untested edge case, not a regression from a prior working state.

## Environment
- **Build**: branch `qa-loop-05-logistics`, based on main `5eab203`
- **Platform**: headless .NET 8 test layer (no Unity Editor in this sandbox); logic is engine-agnostic C# under `src/ProjectAegis.Sim`
- **Scene/Level**: N/A (pure simulation unit)
- **Game State**: Any unit tracked by a `FuelLedger` instance (e.g. any scenario with `Logistics.UsesFuelBurnModel: true`)

## Reproduction Steps
**Preconditions**: A `FuelLedger` instance with `capacityKg = 10_000`, `burnRateKgPerSecond = 80`, and a unit registered via `EnsureUnit("u1")` (full tank).

1. Call `ledger.AdvanceTick("u1", -50.0)` — i.e. supply a negative `deltaSeconds` (an out-of-order or clock-corrected tick, such as a replay re-sync step).
2. Observe the returned `RemainingKg` and `ledger.GetRemainingKg("u1")`.

**Expected Result**: Remaining fuel is clamped to the tank's physical bounds, i.e. `0 <= RemainingKg <= capacityKg` (10,000kg here — the tank cannot hold more than it holds when full, regardless of what a malformed/negative timestep computes).

**Actual Result (before fix)**: `previous - burn` = `10,000 - (80 * -50)` = `10,000 + 4,000` = `14,000kg`, and the old code only floored at zero (`Math.Max(0, previous - burn)`), so it returned `14,000kg` — 4,000kg over the tank's stated capacity. Confirmed via `git stash` (reverting only the production file): the new regression test then fails with `Remaining fuel 14000kg exceeded the 10,000kg tank capacity after a negative-delta tick.`

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Logistics/FuelLedger.cs` — `AdvanceTick` (fixed: `Math.Clamp(previous - burn, 0, _capacityKg)` replacing `Math.Max(0, previous - burn)`)
  - `src/ProjectAegis.Sim.Tests/Logistics/FuelLedgerTests.cs` — new regression test `AdvanceTick_negative_delta_does_not_overfill_tank_beyond_capacity`
- **Related systems**: `ProjectAegis.Delegation.Logistics.FuelTimelineTracker.Drain(...)` is the sole production caller of `AdvanceTick`, itself called only from `ProjectAegis.Delegation.UnityAdapter.Bridge.DelegationBridge.EmitFuelTransitions`, which currently passes a **hardcoded `1.0`** for `deltaSeconds`. So no live code path can trigger this today. However, `AdvanceTick`'s signature (`double deltaSeconds`) is a public contract with no guard against negative input, and the class's own invariant (relied on by `GetRemainingKg`'s unknown-unit fallback returning `_capacityKg`, `EnsureUnit`'s initial fill, and `ResolveBand`'s `remaining/capacity` fraction) assumes `RemainingKg` never exceeds `_capacityKg`. Any future caller wiring in variable tick deltas (e.g. a replay/re-sync feature, explicitly anticipated by the "out-of-order or clock-corrected tick" scenario) would silently violate that invariant without this fix.
- **Possible root cause**: `Math.Max(0, previous - burn)` only enforces the lower bound of the valid fuel range; it does not defend the upper bound (`_capacityKg`) when `burn` is negative (i.e. `deltaSeconds < 0`).

## Evidence
- **Logs**: xUnit failure before fix (production file reverted via `git stash`, test kept):
  ```
  Failed ProjectAegis.Sim.Tests.Logistics.FuelLedgerTests.AdvanceTick_negative_delta_does_not_overfill_tank_beyond_capacity
  Error Message:
   Remaining fuel 14000kg exceeded the 10,000kg tank capacity after a negative-delta tick.
  ```
- **After fix**: same test passes in isolation (`Passed! - Failed: 0, Passed: 1, Total: 1`), and the full `ProjectAegis.Sim.Tests` suite passes 282/282 (281 baseline + 1 new), with no regressions in `ProjectAegis.Delegation.Tests` (251/251) or `ProjectAegis.Delegation.UnityAdapter.Tests` (260/260).
- **Behavior-preservation argument**: for the normal (non-negative `deltaSeconds`) path, `burn >= 0`, so `previous - burn <= previous <= _capacityKg` always holds (given the ledger's own invariant that `previous` never exceeds capacity); `Math.Clamp(x, 0, capacity)` and `Math.Max(0, x)` are therefore identical whenever `x <= capacity`. The fix cannot change behavior for any currently-reachable (`deltaSeconds >= 0`) call.

## Related Issues
- None on file (`production/qa/bugs/` did not previously exist in this worktree).

## Notes
- GitNexus (`.gitnexus/run.cjs`) is not present/initialized in this worktree, so impact analysis for `FuelLedger.AdvanceTick` was performed manually via `grep` across `src/` rather than via the GitNexus MCP/CLI tooling mandated by the repo's `CLAUDE.md`. Manual trace: `FuelLedger.AdvanceTick` <- `FuelTimelineTracker.Drain` (`src/ProjectAegis.Delegation/Logistics/FuelTimelineTracker.cs:37`) <- `DelegationBridge.EmitFuelTransitions` (`src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs:378`, hardcoded `deltaSeconds = 1.0`). No other production callers found. **Risk: LOW** — single call site, current caller always passes `deltaSeconds = 1.0` (never negative), and the fix is provably behavior-preserving on that path (see Evidence above).

## Closure Record
**Closed**: 2026-07-06
**Resolution**: Fixed — `FuelLedger.AdvanceTick` now clamps `RemainingKg` to `[0, _capacityKg]` via `Math.Clamp` instead of only flooring at zero via `Math.Max`, preventing a negative `deltaSeconds` tick from reporting fuel above tank capacity.
**Fix commit / PR**: local commit on `qa-loop-05-logistics` (this loop) — see branch history for hash.
**Verified by**: gameplay-qa-agent (TDD red/green cycle: reverted production file via `git stash`, confirmed new test fails with 14,000kg > 10,000kg capacity; restored fix, confirmed pass; ran full `ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, `ProjectAegis.Delegation.UnityAdapter.Tests` with zero regressions)
**Closed by**: gameplay-qa-agent
**Regression test**: `src/ProjectAegis.Sim.Tests/Logistics/FuelLedgerTests.cs::AdvanceTick_negative_delta_does_not_overfill_tank_beyond_capacity`
**Status**: Closed
