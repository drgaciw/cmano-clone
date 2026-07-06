# Bug Report

## Summary
**Title**: Softmax decision pipeline still gives non-zero selection weight to a "pre-filtered" (score = 0.0) candidate, letting AI agents re-propose Engage after their primary target is already destroyed
**ID**: BUG-zero-scored-candidate-still-selectable
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Fixed (regression test added, fix landed same commit)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-03-delegation)

## Classification
- **Category**: Gameplay (C2 delegation / AI decision logic)
- **System**: `ProjectAegis.Delegation` — stochastic decision pipeline + patrol/engage policy
- **Frequency**: Sometimes (10-50%) — depends on agent personality (`Decisiveness` trait sets softmax temperature) and RNG draw; reproduced deterministically at ~14% draw probability for the "Aggressive" preset (Decisiveness 0.8) and confirmed to trigger within the first handful of seeds in a swept test
- **Regression**: No — appears to be present since `PatrolCandidateEngagePolicy`'s post-kill pre-filter was added (S57-03 AAR remediation, "addresses game-players-report Topic 1"); the pre-filter never actually worked as intended and was not previously covered by any test that exercises `DecisionPipeline.Choose` with the destroyed-primary candidate set

## Environment
- **Build**: worktree `qa-r2-03-delegation`, main HEAD `e2e1342` as of 2026-07-06 (.NET simulation layer, no Unity Editor involved — reproduced purely via `dotnet test`)
- **Platform**: N/A (pure C# sim/delegation logic, exercised headlessly)
- **Scene/Level**: N/A — reproduced with a synthetic `AgentController`-style call into `DecisionPipeline.Choose` using the real `PatrolCandidateEngagePolicy` candidate set
- **Game State**: An AI-controlled unit whose primary hostile (or primary blue-force, in `EngagePrimaryMode.BlueForce` mode) contact has just been confirmed destroyed

## Reproduction Steps
**Preconditions**: An `AgentController` is bound to `PatrolCandidateEngagePolicy` and its primary target (`PrimaryHostileDestroyed` / `PrimaryBlueForceContactDestroyed`) has just flipped to `true`.

1. `PatrolCandidateEngagePolicy.GenerateCandidates` is called with `primaryDestroyed == true`. Per its own comment ("Pre-filter: score Engage at 0 ... when primary target confirmed destroyed. This prevents re-engagement proposals in AAR"), it returns `[Hold: 1.0, Move: 0.8, Engage: 0.0]` instead of omitting `Engage`.
2. `AgentController.TryDecide` passes this candidate list into `DecisionPipeline.Choose`, which converts scores to softmax weights via `Math.Exp(c.Score / temperature)`.
3. For the zero-scored `Engage` candidate, `Math.Exp(0.0 / temperature) == 1.0` — a normal, non-negligible weight, not the ~0 the pre-filter comment implies. With the "Aggressive" personality preset (`Decisiveness = 0.8`, so `temperature = 0.8` when not overloaded), the weights are `Hold ≈ 3.49`, `Move ≈ 2.72`, `Engage = 1.0` (sum ≈ 7.21) — i.e. **Engage is drawn roughly 1 time in 7 (~14%)**, not "never."

**Expected Result**: Once the primary target is confirmed destroyed, `DecisionPipeline.Choose` should never select `Engage` (or any candidate a policy has explicitly zeroed out) as long as at least one positively-scored alternative (`Hold`/`Move`) is available — that is the entire point of the S57-03 AAR remediation.

**Actual Result** (before fix): `Engage` remained selectable with real, personality-dependent probability (double digits for higher-`Decisiveness` presets), silently defeating the "no re-engagement proposals" guarantee the pre-filter comment claims to provide. In a live game this reproduces the exact AAR complaint the pre-filter was written to fix: an agent re-engaging (or attempting to) a target that is already destroyed.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs` (`Choose`) — root cause: softmax weighting treats a score of `0.0` as a normal, non-excluding weight (`exp(0) = 1`) rather than an exclusion signal
  - `src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs` (`GenerateCandidates`) — the caller whose "score at 0 to suppress" contract `DecisionPipeline.Choose` silently failed to honor
  - `src/ProjectAegis.Delegation/Controllers/AgentController.cs` (`TryDecide`) — sole production call site of `DecisionPipeline.Choose`; carries the effect of the fix into every AI decision tick
- **Related systems**: personality/trait system (`Decisiveness` sets softmax temperature — the bug's severity scales with this trait), attention/overload degradation (`NarrowedFocus` truncates the pool to the top 2 candidates by score, independently of this bug)
- **Root cause**: `DecisionPipeline.Choose` has no concept of "excluded" candidates — it treats every `ScoredIntent.Score` purely as a softmax logit. A policy that wants to suppress a candidate without removing it from the logged/inspectable candidate set (as `PatrolCandidateEngagePolicy` intentionally does, to preserve the full set in decision logs) has no way to drive that candidate's *selection weight* to zero, because `Math.Exp(0) = 1`, not `0`. Only a large negative score (`-infinity`-ish) would actually zero out the weight, and no policy in the codebase does that.

## Evidence
- **New failing-then-passing test**: `ProjectAegis.Delegation.Tests.Decision.DecisionPipelineTests.Choose_never_selects_a_zero_scored_candidate_when_a_positive_scored_alternative_exists`
  (`src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs`)
  - Before fix: **Failed** at `salt = 4` (first failing seed in a 500-seed sweep) — `Assert.That(choice.Chosen.Kind, Is.Not.EqualTo(OrderKind.Engage))` failed because `Engage` was drawn despite scoring `0.0`.
  - After fix: **Passed** — all 500 seeds.
- **Full suite before/after**: `ProjectAegis.Delegation.Tests` 253/253 (baseline) -> 254/254 (253 pre-existing + 1 new, all green). `ProjectAegis.Delegation.UnityAdapter.Tests` 261/261 -> 261/261 (no change, no regressions — no Baltic golden scenario happens to exercise the destroyed-primary + `PatrolCandidateEngagePolicy` + RNG-draws-Engage combination within its fixed tick counts, so no golden fingerprint needed updating). `ProjectAegis.MissionEditor.Cli.Tests`: 1 pre-existing, unrelated failure (`BranchIntegrationPhase0SmokeTests.Phase0_smoke_script_exists_and_passes_quick_mode`, a bash-script + full-solution-build smoke test) reproduces identically with the fix `git stash`-ed out (i.e. against unmodified baseline code) — confirmed not a regression caused by this change; all other MissionEditor.Cli.Tests (72/73 in this run, including the 34 `Scenario*`-filtered tests that exercise sim/delegation-adjacent code) pass unchanged.

## Impact Analysis (manual, GitNexus unreachable in this isolated worktree)
`DecisionPipeline.Choose` has exactly one production caller: `AgentController.TryDecide` (verified via `grep -rn "DecisionPipeline\.Choose\|DecisionPipeline\b"` across `src/`). `AgentController.TryDecide` is invoked once per tick per agent-controlled target from `DelegationOrchestrator.Tick`. Blast radius is therefore: every AI agent decision, gated on candidate score sign. All existing policies (`StubPatrolPolicy.DefaultCandidates`, the "normal" `PatrolCandidateEngagePolicy` candidate set, `EngageOnlyPolicy` used in `PolicyDenialLogTests`) only ever emit strictly positive scores, so the new filter (`pool.Where(c => c.Score > 0)`, applied only when at least one positive-scored candidate exists) is a no-op for them — confirmed empirically by the unchanged 261/261 `UnityAdapter.Tests` and 253 pre-existing `Delegation.Tests` all remaining green. Risk: **LOW** — single call site, change is additive/guarded (never empties the pool), and is exercised transparently by every existing non-destroyed-primary scenario without altering their outcomes.

## Related Issues
- `production/qa/bugs/BUG-double-take-control-drops-queued-orders.md` (round 1) — different bug class (control-handoff order loss vs. decision-selection weighting); no overlap in files or symbols touched.
- Adjacent code comment: `PatrolCandidateEngagePolicy.GenerateCandidates` explicitly states the intent ("This prevents re-engagement proposals in AAR (addresses game-players-report Topic 1)") that this bug silently violated — worth flagging to whoever owns the AAR/game-players-report backlog that Topic 1 was not actually resolved by the original S57-03 change until this fix.

## Notes
Fix implemented as a minimal guard at the top of `DecisionPipeline.Choose`: if the candidate pool contains both at least one positive-scored candidate and at least one non-positive-scored (`<= 0`) candidate, drop the non-positive ones before computing softmax weights (applied before the existing `NarrowedFocus` top-2 truncation, so it composes correctly with attention-overload behavior). If every candidate is non-positive (an edge case no current policy produces), the pool is left untouched so a choice can still be made rather than throwing on an empty pool. This is a general fix (any future policy that "scores to suppress" a candidate now gets the exclusion it intends) but the only production candidate set it currently changes behavior for is `PatrolCandidateEngagePolicy`'s destroyed-primary case.
