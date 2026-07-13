# Bug Report

## Summary
**Title**: Replay fingerprint renders IEEE-754 negative zero (`-0.0`) differently than positive zero (`0.0`), breaking the "same simulated value -> same fingerprint text" determinism contract
**ID**: BUG-fingerprint-negative-zero
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-loop-07-determinism-replay)

## Classification
- **Category**: Gameplay (simulation determinism / replay fidelity)
- **System**: `ProjectAegis.Delegation.Decision.FingerprintFloat` — the invariant-culture float formatter used by `DecisionLog.ComputeFingerprint()` / `ScoredIntentFingerprint.Format()` for every fractional field embedded in the replay fingerprint (RngDraw, ScoredIntent.Score, EngagementOutcome.PkDraw, FuelBurn.DeltaKg/RemainingFuelKg, PlatformDamageChange.PreviousHpPct/NewHpPct, and decision-log SimTime).
- **Frequency**: Rare today (no currently-pinned golden fixture happens to hit the edge case — confirmed no `-0` token exists in any `tests/regression/*.txt`), but is a **latent, always-there** hazard: it triggers any time an arithmetic path yields negative zero for a "no change" quantity instead of positive zero.
- **Regression**: No — `FingerprintFloat` was introduced by the immediately preceding commit (`2abef95`, "format replay-fingerprint floats invariantly to remove determinism hazard") specifically to make the fingerprint culture/platform stable. That commit did not add any unit tests for the new class itself (only golden-file regeneration), so this edge case was never exercised. This bug reintroduces the exact same class of hazard the prior fix was meant to close, from a different angle (sign of zero instead of decimal separator / round-trip tail).

## Environment
- **Build**: branch `qa-loop-07-determinism-replay`, based on main HEAD `5eab203`
- **Platform**: linux-x64, .NET 8.0 (verified interactively; per .NET's IEEE-754 formatting rules this reproduces identically on any conforming .NET runtime/OS)
- **Scene/Level**: N/A — plain .NET simulation layer (`src/ProjectAegis.Delegation`), no Unity Editor involved
- **Game State**: Any tick where a fractional replay-fingerprint field (fuel delta, HP delta, score, RNG/Pk draw, SimTime) evaluates to negative zero instead of positive zero

## Reproduction Steps
**Preconditions**: none — pure unit-level repro against the public `DecisionLog` API.

1. Confirm in isolation (interactive `dotnet run`):
   ```csharp
   double negZero = -0.0, posZero = 0.0;
   negZero == posZero;                              // true
   negZero.ToString("0.######", CultureInfo.InvariantCulture); // "-0"
   posZero.ToString("0.######", CultureInfo.InvariantCulture); // "0"
   ```
2. Build two `DecisionLog`s representing the *same* simulated state ("no fuel burned this tick, 500kg remaining") — one where `DeltaKg` happens to be `0.0`, one where it is `-0.0` (e.g. because a future/alternate arithmetic path computes the delta as `-(burn - already-zero)` instead of `(remaining - previous)`):
   ```csharp
   logA.AppendFuelBurn(new FuelBurnRecord(0, 10.0, 10, new TargetId("u1"), 0.0, 500.0));
   logB.AppendFuelBurn(new FuelBurnRecord(0, 10.0, 10, new TargetId("u1"), -0.0, 500.0));
   ```
3. Call `ComputeFingerprint()` on both logs and compare.

**Expected Result**: Both fingerprints are byte-identical (`0.0` and `-0.0` represent the same simulated fact: "no change").
**Actual Result (pre-fix)**:
```
"FuelBurn|1|10|10|u1|0|500\n"    (positive zero)
"FuelBurn|1|10|10|u1|-0|500\n"   (negative zero)
```
Different text -> `OrderLogReplayFingerprint.ComputeSha256Hex` produces a different SHA-256 -> a replay-verify gate would report a false-positive divergence (or, worse, two runs that are semantically identical would fail to be recognized as matching) purely because of which side of zero a floating-point op landed on — the exact "hash changes without content change" failure mode called out for this QA pass.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Delegation/Decision/FingerprintFloat.cs` (root cause — `Format`/`Time` do not normalize the sign of zero before calling `double.ToString`)
  - `src/ProjectAegis.Delegation/Decision/DecisionLog.cs` (`ComputeFingerprint()` / `FormatPayload()` — 6 call sites feeding `FingerprintFloat.Format`, 1 feeding `FingerprintFloat.Time`)
  - `src/ProjectAegis.Delegation/Decision/ScoredIntentFingerprint.cs` (1 call site feeding `FingerprintFloat.Format(a.Score)`)
- **Related systems**: `tests/regression/*.txt` golden `FINGERPRINT_SHA256` values (verified none currently trip this edge case); `OrderLogReplayFingerprint.ComputeSha256Hex`; `BalticReplayHarness.Result.FingerprintSha256`.
- **Possible root cause**: `FingerprintFloat.Format`/`.Time` call `value.ToString(..., CultureInfo.InvariantCulture)` directly. `double.ToString` preserves the IEEE-754 sign bit of zero (`-0.0` renders as `"-0"`), but `-0.0 == 0.0` is `true` in the simulation's own equality semantics, so any code path that can produce `-0.0` for a zero-valued delta/score/draw (unary negation of zero, multiplication of zero by a negative factor, or a future refactor that computes a difference in the opposite operand order) silently changes the fingerprint text without changing the simulated outcome.

## Evidence
- **Test added (red -> green)**: `src/ProjectAegis.Delegation.Tests/Decision/FingerprintFloatNegativeZeroTests.cs` — `FuelBurn_fingerprint_treats_negative_zero_delta_same_as_positive_zero`.
  - **Before fix**: FAILED — `Expected: "FuelBurn|1|10|10|u1|0|500\n"` vs `"FuelBurn|1|10|10|u1|-0|500\n"`.
  - **After fix**: PASSED.
- **Impact check (manual, GitNexus not reachable inside this isolated worktree — `.gitnexus/` directory absent)**: grepped all callers of `FingerprintFloat.Format`/`.Time` across `src/`; exactly 8 call sites, all confined to `ScoredIntentFingerprint.cs` and `DecisionLog.cs` in `ProjectAegis.Delegation.Decision`. No other assembly references the (internal) class. Blast radius: LOW — pure text-formatting change, no branching logic depends on the formatted string's content besides fingerprint/hash equality checks.
- **Golden-file safety check**: `grep -n -- "-0[|.]" tests/regression/*.txt` returned no matches — no currently-pinned golden fixture exercises this edge case, so the fix requires **no golden regeneration**.
- **Regression sweep (before -> after fix, same commit)**:
  - `ProjectAegis.Delegation.Tests`: 251/251 -> 252/252 (251 pre-existing + 1 new)
  - `ProjectAegis.Sim.Tests`: 281/281 -> 281/281 (unaffected, unchanged)
  - `ProjectAegis.Delegation.UnityAdapter.Tests` (incl. `ReplayGoldenSuiteTests` 6/6): 260/260 -> 260/260 (unaffected, unchanged — confirms no pinned Baltic golden/hash was touched)
  - `ProjectAegis.Data.Tests`: 476/476 -> 476/476 (unaffected, unchanged)
  - `ProjectAegis.Data.Excel.Tests`: 5/5 -> 5/5 (unaffected, unchanged)
  - `ProjectAegis.MissionEditor.Cli.Tests`: 70/70 passing both before and after (excluding `BranchIntegrationPhase0SmokeTests.Phase0_smoke_script_exists_and_passes_quick_mode`, which fails identically on the unmodified baseline in this sandbox — confirmed via `git stash` — due to a pre-existing sandbox/environment issue, unrelated to this change)

## Related Issues
- Directly follows on from commit `2abef95` ("fix(delegation): format replay-fingerprint floats invariantly to remove determinism hazard"), which introduced `FingerprintFloat` but left it untested and did not consider the sign-of-zero case.
- Distinct from the sibling `PdDetectionContactSimulator.ApplyTargetKill` missing-ordinal-sort hazard (owned by loop `qa-loop-01-sensors`) — no overlap; that fix targets collection ordering, this one targets floating-point sign-of-zero formatting.

## Notes
Fix: `FingerprintFloat.Format`/`.Time` now normalize any value that is `== 0` (covers both `+0.0` and `-0.0`, since `NaN == 0` is `false` so `NaN` passes through unchanged) to `0.0` before calling `ToString`, guaranteeing the fingerprint is a pure function of simulated value rather than of the IEEE-754 sign bit of an equivalent zero.
