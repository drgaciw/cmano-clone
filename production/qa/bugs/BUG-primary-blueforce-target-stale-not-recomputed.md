# Bug Report

## Summary
**Title**: `PdDetectionContactSimulator.EmitStaleLosses` never recomputes `PrimaryBlueForceTargetId` when the contact backing it goes stale-Lost, leaving a phantom "primary blue-force" pointer after sensor coverage is actually gone
**ID**: BUG-PRIMARY-BLUEFORCE-STALE-0001
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-01-sensors)

## Classification
- **Category**: Gameplay (sensor/detection contact-picture correctness; red-side targeting data feed)
- **System**: Sensor & detection lifecycle — `ProjectAegis.Sim.Sensors.PdDetectionContactSimulator` (`EmitStaleLosses` / `RecomputePrimary`)
- **Frequency**: Always, in any scenario where the only contact tracking the current primary blue-force target goes Lost via the stale/missed-ticks path (rather than via `ApplyTargetKill`/`ApplyTargetBdaLost`)
- **Regression**: Unknown/longstanding — the asymmetry between the hostile-target check and the (missing) blue-force-target check has existed since `PrimaryBlueForceTargetId`/`UpdatePrimary`'s blue-force branch was added; prior test coverage of blue-force primary selection never combined it with a stale-loss scenario, so it was never caught.

## Environment
- **Build**: branch `qa-r2-01-sensors`, worktree HEAD at repo commit `e2e1342` (round-1 fixes, including `BUG-KILL-ORDER-0001`, already merged into `main` and present in this worktree)
- **Platform**: linux, .NET 8.0 SDK, `dotnet test` (no Unity Editor available in this sandbox)
- **Scene/Level**: N/A — pure sim-layer bug, exercised via `ProjectAegis.Sim.Tests`
- **Game State**: A scenario with a blue-force (friendly) detection trial (e.g. a red-side IRST/EO track on a blue unit, `RequiresActiveRadar: false`) whose single supporting contact ages out via the stale-threshold missed-ticks mechanism, with no hostile-target contact simultaneously lost

## Reproduction Steps
**Preconditions**: A `PdDetectionContactSimulator` constructed with exactly one `ScenarioDetectionTrial` whose `TargetId` is a registered blue-force unit (`BalticV3SideRegistry.IsBlueForceUnit(...) == true`, e.g. `"u1"`), `basePd: 1.0`, `RequiresActiveRadar: false`, and `ScenarioContactLifecycle(StaleThresholdTicks: 1)`.

1. Call `sim.Tick(1, 1.0)` — the contact is Detected, so `sim.PrimaryBlueForceTargetId == "u1"`.
2. Call `sim.Tick(2, 2.0)` — because this contact is now already-detected, it is excluded from re-rolling and accumulates a missed tick; with `StaleThresholdTicks: 1` this immediately crosses the threshold and the contact transitions `Detected -> Lost` (the same stale-to-Lost mechanism already exercised, and accepted as correct, by `PdContactStaleTests.Contact_goes_lost_after_stale_threshold_missed_ticks`). `sim.ActiveCount` becomes `0`.
3. Inspect `sim.PrimaryBlueForceTargetId`.

**Expected Result**: `PrimaryBlueForceTargetId` is recomputed and becomes `null` (no contact is tracking any blue-force unit any more) — matching the guarantee `ApplyTargetKill`/`ApplyTargetBdaLost` already provide (both call `RecomputePrimary()` unconditionally whenever any contact for the given target is lost, which the existing test `PdDetectionContactSimulatorTests.Primary_target_ignores_non_hostile_detections` verifies for the hostile-target case via `ApplyTargetKill`).

**Actual Result** (pre-fix): `PrimaryBlueForceTargetId` still returns `"u1"` even though `ActiveCount == 0` and no contact observes it any more — a stale/phantom pointer to a target the sensor picture no longer actually supports.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs` — `EmitStaleLosses` (lines ~227-236 pre-fix), the `foreach (var contactId in lost)` block only ever checked `_primaryTargetId` (the hostile-side primary), never `_primaryBlueForceTargetId`.
- **Related systems / blast radius** (manual Grep-based impact check — GitNexus index/CLI is not reachable from this isolated worktree, per repo policy):
  - `PrimaryBlueForceTargetId` (public property) is read externally by exactly one production call site: `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs:411`, inside `PrimaryBlueForceContactId` — used to surface the currently-tracked friendly target (consumed for red-side engagement targeting decisions per the existing test name `Primary_target_tracks_blue_force_for_red_side_engage`). A stale pointer here means downstream red-side AI/targeting logic could keep "seeing" a friendly unit as trackable after the sensor picture has actually lost it.
  - `EmitStaleLosses` is private and called from exactly one place, `Tick()`, within the same class — no other internal callers.
  - `RecomputePrimary()` is private with two pre-existing call sites (`ApplyTargetBdaLost`, `ApplyTargetKill`), both already unconditional; the fix adds a third, conditional, call site inside `EmitStaleLosses`.
  - Risk assessed as LOW-MEDIUM: the change only adds a recompute in a case that previously never recomputed (strictly more correct), and only fires when a lost contact's target matches the current blue-force primary — it cannot affect the hostile-target code path or any scenario without a blue-force detection trial. Full regression run below confirms zero fallout in `ProjectAegis.Sim.Tests`, `ProjectAegis.Delegation.Tests`, and `ProjectAegis.Delegation.UnityAdapter.Tests` (the only project besides `Sim.Tests` that references `PdDetectionContactSimulator`/`PrimaryBlueForceTargetId` transitively via `BalticReplayHarness`).
- **Possible root cause**: `EmitStaleLosses`'s recompute-trigger check was written only against `_primaryTargetId` (hostile) when the stale-to-Lost FSM was introduced (commit `5a8b7d1`), predating the blue-force primary-tracking branch of `UpdatePrimary` (`_primaryBlueForceTargetId`, commit context in `Primary_target_tracks_blue_force_for_red_side_engage`). When the blue-force branch was added, `RecomputePrimary()`'s two other call sites (`ApplyTargetKill`/`ApplyTargetBdaLost`) were already unconditional and so needed no change, but `EmitStaleLosses`'s conditional trigger was never updated to also check the blue-force pointer — the same class of "paired code paths where only one side of a duplicated condition got the update" issue as `BUG-KILL-ORDER-0001` from round 1, but manifesting in a different method/condition (primary-target recompute triggering vs. transition-ordering), not the same bug re-found.

## Evidence
- **New failing test (red)**: `ProjectAegis.Sim.Tests.Sensors.PdContactPrimaryBlueForceStaleTests.Stale_loss_of_only_blue_force_contact_clears_primary_blue_force_target`
  - Path: `src/ProjectAegis.Sim.Tests/Sensors/PdContactPrimaryBlueForceStaleTests.cs`
  - Failure before fix:
    ```
    Assert.Null() Failure: Value is not null
    Expected: null
    Actual:   "u1"
    ```
- **Fix**: in `EmitStaleLosses`, replaced the hostile-only recompute condition with a check against both `_primaryTargetId` and `_primaryBlueForceTargetId`:
  ```csharp
  foreach (var contactId in lost)
  {
      _detectedContacts.Remove(contactId);
      var lostTargetId = _trialsByContactId[contactId].TargetId;
      if ((_primaryTargetId != null && lostTargetId == _primaryTargetId) ||
          (_primaryBlueForceTargetId != null && lostTargetId == _primaryBlueForceTargetId))
      {
          RecomputePrimary();
      }
  }
  ```
- **Post-fix (green)**: new test passes; `dotnet test src/ProjectAegis.Sim.Tests` -> 286/286 passed (was 285/285 baseline + 1 new test); `ProjectAegis.Delegation.Tests` 253/253 (unchanged); `ProjectAegis.Delegation.UnityAdapter.Tests` 261/261 (unchanged) — confirming no regression in the `BalticReplayHarness` consumer of `PrimaryBlueForceTargetId`.

## Related Issues
- Same general class of "asymmetric duplicated-condition" bug as `production/qa/bugs/BUG-kill-transition-order-nondeterminism.md` (round 1), but a distinct symptom/method — that bug was about transition emission ordering in `ApplyTargetKill`; this one is about primary-pointer recompute triggering in `EmitStaleLosses`. Not a re-find of the same bug.

## Notes
- Impact analysis performed manually (GitNexus index/CLI not reachable inside this isolated worktree — per repo policy, no `node .gitnexus/run.cjs` available here). Grep-based caller search covered `EmitStaleLosses`, `RecomputePrimary`, `_primaryBlueForceTargetId`, and `PrimaryBlueForceTargetId` across `src/`; results summarized in Technical Context above. No HIGH/CRITICAL risk found.
- `ProjectAegis.MissionEditor.Cli.Tests` was not re-run: a Grep of that project found zero references to `PdDetectionContactSimulator`, `PrimaryBlueForceTargetId`, or `EmitStaleLosses`, so it is not exercised by this change; skipped to avoid its known >45s runtime for a project with no code-path overlap.
- During investigation, a second, larger latent issue was also observed in the same class (`EmitStaleLosses`/`RollTick`'s `alreadyDetectedContactIds` filter): once a contact is first detected, its trial is never rolled again, so the `existing.MissedTicks = 0` "reaffirm" branch inside `Tick()` is unreachable dead code, meaning every contact unconditionally expires to Lost exactly `StaleThresholdTicks` ticks after first detection regardless of continued 100%-certain sensor coverage. This is provably dead code (confirmed by tracing `RollTick`'s `alreadyDetectedContactIds` filter, present since the very first commit introducing the detection loop, `ab1c056`, predating the stale-FSM feature `5a8b7d1` that added the now-unreachable reset). However, this exact behavior (fixed-schedule expiry regardless of Pd) is also what several *existing, already-accepted* tests assert as correct (`PdContactStaleTests.Contact_goes_lost_after_stale_threshold_missed_ticks`, `Lost_transition_uses_actual_previous_state_not_hardcoded_detected`, both using `basePd: 1.0`), so a real fix would change long-shipped, already-tested behavior and touches the RNG draw sequence feeding `DetectionWorldHash`/replay goldens — a much larger, cross-cutting change than fits a single QA-loop fix. Flagging this explicitly as a recommended follow-up design/product discussion (is the hard timeout intentional MVP behavior, or should continuously-covered contacts be sticky?) rather than silently changing accepted behavior in this pass.
