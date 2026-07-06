# Bug Report

## Summary
**Title**: `ApplyTargetKill` emits contact-lost transitions in non-deterministic dictionary order, breaking replay determinism when multiple contacts track the same killed target
**ID**: BUG-KILL-ORDER-0001
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-05
**Reporter**: gameplay-qa-agent (qa-loop-01-sensors)

## Classification
- **Category**: Gameplay (simulation determinism / replay integrity)
- **System**: Sensor & detection lifecycle — `ProjectAegis.Sim.Sensors.PdDetectionContactSimulator`
- **Frequency**: Sometimes (10-50%) — only manifests when 2+ contacts (from different observers/sensors) are simultaneously tracking the same target at the moment it is destroyed
- **Regression**: Unknown — appears to have existed since `ApplyTargetKill` was introduced alongside `ApplyTargetBdaLost`; likely never triggered because prior test coverage only ever destroyed single-contact targets

## Environment
- **Build**: branch `qa-loop-01-sensors`, worktree HEAD at repo commit `5eab203`
- **Platform**: linux, .NET 8.0 SDK, `dotnet test` (no Unity Editor available in this sandbox)
- **Scene/Level**: N/A — pure sim-layer bug, exercised via `ProjectAegis.Sim.Tests`
- **Game State**: A scenario where multiple sensors (e.g. two different observing units/radars) each hold an independent contact track (`ContactId`) against the same underlying `TargetId`, and that target is destroyed by combat (kill), not BDA-loss

## Reproduction Steps
**Preconditions**: A `PdDetectionContactSimulator` with two `ScenarioDetectionTrial`s pointing at the same `TargetId` but different `ContactId`s, sorted (by the mandatory Observer→Sensor→Target ordering) so that the dictionary-insertion order of the two contacts differs from their `ContactId`'s ordinal string order.

1. Construct trials:
   - `("u1", "s1", "hostile-1", "cB", basePd: 1.0)`
   - `("u2", "s1", "hostile-1", "cA", basePd: 1.0)`
   (Sorting by ObserverId places `u1`'s trial — contact `"cB"` — before `u2`'s trial — contact `"cA"` — so `"cB"` is inserted into the internal `_tracks` dictionary first, even though `"cA"` < `"cB"` ordinally.)
2. Call `sim.Tick(1, 1.0)` so both contacts become Detected.
3. Call `sim.ApplyTargetKill(2, 2.0, "hostile-1")`.
4. Inspect the `ContactId` order of the returned `ContactTransition` list.

**Expected Result**: Transitions emitted in deterministic ordinal `ContactId` order — `["cA", "cB"]` — matching the ordering guarantee that `ApplyTargetBdaLost` already provides via its explicit `.OrderBy(contactId => contactId, StringComparer.Ordinal)`.

**Actual Result** (pre-fix): Transitions emitted as `["cB", "cA"]` — the raw `Dictionary<string, ContactTrack>.Keys` enumeration/insertion order, not ordinal order. This is an implementation-detail ordering (not part of any documented `Dictionary` contract) that happens to track detection-roll order rather than a stable sort key.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs` — `ApplyTargetKill` (missing `.OrderBy`) vs. `ApplyTargetBdaLost` (has `.OrderBy`), lines ~285-367
- **Related systems**:
  - `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` line ~312 — feeds `ApplyTargetKill`'s output directly into `bridge.Orchestrator.OrderLog.AppendContactTransition`, i.e. the deterministic replay/order-log stream that `tests/regression/replay-golden-*.txt` goldens are built from.
  - The codebase explicitly documents a determinism discipline elsewhere in this same class (`SortedSet<string> _detectedContacts` — "P2 allocation follow-up (S37-09): SortedSet for deterministic ordinal iteration without per-tick OrderBy/alloc") and in `DeterministicDetectionLoop.SortTrials` (explicit ObserverId→SensorId→TargetId sort). `ApplyTargetKill` breaks that same discipline.
- **Possible root cause**: `ApplyTargetBdaLost` and `ApplyTargetKill` were implemented as near-duplicate methods; the ordinal `.OrderBy` was added to one and not the other, likely a copy/paste omission.

## Evidence
- **New failing test (red)**: `ProjectAegis.Sim.Tests.Sensors.PdContactKillTests.Kill_with_multiple_contacts_on_same_target_emits_transitions_in_ordinal_contact_order`
  - Path: `src/ProjectAegis.Sim.Tests/Sensors/PdContactKillTests.cs`
  - Failure before fix:
    ```
    Assert.Equal() Failure: Collections differ
            ↓ (pos 0)
    Expected: ["cA", "cB"]
    Actual:   ["cB", "cA"]
            ↑ (pos 0)
    ```
- **Fix**: added `.OrderBy(contactId => contactId, StringComparer.Ordinal)` to the `lostContacts` projection in `ApplyTargetKill`, mirroring `ApplyTargetBdaLost` exactly.
- **Post-fix (green)**: same test passes; `dotnet test src/ProjectAegis.Sim.Tests` → 282/282 passed (was 281/281 baseline + 1 new test); `ProjectAegis.Delegation.Tests` 251/251; `ProjectAegis.Delegation.UnityAdapter.Tests` 260/260 (both unchanged from baseline, confirming no regression in replay-golden-consuming code).

## Related Issues
- None filed prior to this session.

## Notes
- Impact analysis performed manually (GitNexus index not reachable inside this isolated worktree — no `.gitnexus/` directory present; noting this explicitly per repo policy). Grep-based caller search for `ApplyTargetKill` found exactly three call sites: two unit tests (single-contact scenarios, unaffected by the ordering fix) and one production call site (`BalticReplayHarness.cs`, forwards transitions into the deterministic `OrderLog`). Risk assessed as MEDIUM prior to the fix (correctness/determinism bug in a replay-critical path, but narrow trigger condition); confirmed LOW residual risk post-fix since the change is a pure ordering fix that is a no-op for the common single-contact-per-target case and all existing suites remain green.
- This is the same category of fix already applied by this codebase to `ApplyTargetBdaLost`; recommend a follow-up audit for any other paired "kill vs. non-kill" lifecycle methods that might share this omission.
