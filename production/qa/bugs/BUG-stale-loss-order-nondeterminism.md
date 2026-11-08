# Bug Report

## Summary
**Title**: `PdDetectionContactSimulator.EmitStaleLosses` emits stale-timeout contact-lost transitions in non-deterministic dictionary order, breaking replay determinism when 2+ contacts go stale on the same tick
**ID**: BUG-STALE-LOSS-ORDER-0001
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-07-determinism-replay)

## Classification
- **Category**: Gameplay (simulation determinism / replay integrity)
- **System**: Sensor & detection lifecycle — `ProjectAegis.Sim.Sensors.PdDetectionContactSimulator`
- **Frequency**: Sometimes (10-50%) — only manifests when 2+ contacts (tracked by different observers) become "stale" (missed-tick threshold exceeded) on the exact same tick
- **Regression**: No — pre-existing since `EmitStaleLosses` was introduced; this is a sibling method to the already-fixed `ApplyTargetKill` (BUG-KILL-ORDER-0001, fixed on `main`) and `ApplyTargetBdaLost` (which already had the correct ordinal sort), but was not covered by that prior fix or its review.

## Environment
- **Build**: branch `qa-r2-07-determinism-replay`, worktree HEAD at repo commit `e2e1342`
- **Platform**: linux-x64, .NET 8.0 SDK, `dotnet test` (no Unity Editor available in this sandbox)
- **Scene/Level**: N/A — pure sim-layer bug, exercised via `ProjectAegis.Sim.Tests`
- **Game State**: A scenario where 2+ observers each hold an independent contact track (`ContactId`) against different targets (or the same target), all contacts become Detected on the same tick, and the stale-timeout threshold is reached for more than one of them simultaneously (no further detection rolls occur for any of them, e.g. due to EMCON/jamming loss or simply because `DeterministicDetectionLoop.RollTick` never re-rolls an already-detected contact)

## Reproduction Steps
**Preconditions**: Two `ScenarioDetectionTrial`s sorted (by the mandatory ObserverId→SensorId→TargetId ordering used by `DeterministicDetectionLoop.SortTrials`) so that the dictionary-insertion order of the two contacts into `PdDetectionContactSimulator._tracks` differs from their `ContactId`'s ordinal string order.

1. Construct trials:
   - `("u1", "s1", "hostile-1", "cB", basePd: 1.0)`
   - `("u2", "s1", "hostile-2", "cA", basePd: 1.0)`
   (Sorting by ObserverId places `u1`'s trial — contact `"cB"` — before `u2`'s trial — contact `"cA"` — so `"cB"` is inserted into the internal `_tracks` dictionary first on tick 1, even though `"cA"` < `"cB"` ordinally.)
2. Construct the simulator with `StaleThresholdTicks: 1`.
3. Call `sim.Tick(1, 1.0)` — both contacts become Detected. Once detected, `DeterministicDetectionLoop.RollTick` never re-rolls them (it skips any `ContactId` already in `alreadyDetectedContactIds`), so both accumulate missed ticks from here on.
4. Call `sim.Tick(2, 2.0)` — both contacts simultaneously cross the stale threshold and are emitted as `Lost` in the same call.
5. Inspect the `ContactId` order of the `Lost` transitions in the returned list.

**Expected Result**: Transitions emitted in deterministic ordinal `ContactId` order — `["cA", "cB"]` — matching the ordering guarantee `ApplyTargetKill` and `ApplyTargetBdaLost` already provide via explicit `.OrderBy(contactId => contactId, StringComparer.Ordinal)`.

**Actual Result (pre-fix)**: Transitions emitted as `["cB", "cA"]` — the raw `Dictionary<string, ContactTrack>` enumeration/insertion order (tick-1 detection order), not ordinal order. Since `BalticReplayHarness` appends every transition returned by `pdSim.Tick(...)` directly to `bridge.Orchestrator.OrderLog` with no re-sort (`BalticReplayHarness.cs` lines ~277-304), this insertion-order dependency leaks straight into the deterministic order log / replay fingerprint stream.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs` — `EmitStaleLosses` (missing `.OrderBy`, lines ~188-225 pre-fix) vs. `ApplyTargetBdaLost`/`ApplyTargetKill` (both already sort)
- **Related systems**:
  - `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` line ~256-304 — calls `pdSim.Tick(simTick, harness.SimTime)` (which internally calls `EmitStaleLosses`) and appends every returned transition straight into `bridge.Orchestrator.OrderLog.AppendContactTransition`, i.e. the deterministic replay/order-log stream that `tests/regression/replay-golden-*.txt` goldens are built from.
  - Same determinism discipline documented elsewhere in this class (`SortedSet<string> _detectedContacts`, `DeterministicDetectionLoop.SortTrials`) that `EmitStaleLosses` was not brought in line with.
- **Possible root cause**: `EmitStaleLosses` was implemented independently of `ApplyTargetBdaLost`/`ApplyTargetKill` (a third, distinct contact-loss path — timeout-based rather than event-based) and simply enumerated `_tracks` directly rather than sorting candidate `ContactId`s ordinally first, the same omission class as BUG-KILL-ORDER-0001 but in a sibling method never touched by that fix.

## Evidence
- **Test added (red -> green)**: `src/ProjectAegis.Sim.Tests/Sensors/PdContactStaleTests.cs` — `Stale_loss_with_multiple_simultaneous_contacts_emits_transitions_in_ordinal_contact_order`.
  - **Before fix**: FAILED —
    ```
    Assert.Equal() Failure: Collections differ
            ↓ (pos 0)
    Expected: ["cA", "cB"]
    Actual:   ["cB", "cA"]
            ↑ (pos 0)
    ```
  - **After fix**: PASSED.
- **Fix**: `EmitStaleLosses` now builds `candidateContactIds` via `_tracks.Keys.OrderBy(contactId => contactId, StringComparer.Ordinal).ToArray()` and iterates that ordered array (looking up each track by key) instead of enumerating the `_tracks` dictionary directly — mirroring `ApplyTargetBdaLost`/`ApplyTargetKill` exactly.
- **Impact check (manual, GitNexus not reachable inside this isolated worktree — per repo policy, `.gitnexus/` not usable from this worktree)**: grepped all callers of `PdDetectionContactSimulator` construction and its `Tick(...)` entry point across `src/`. Exactly one production call site drives this code path in a replay-relevant way: `BalticReplayHarness.cs`, which forwards every transition from `pdSim.Tick(...)` directly into the deterministic `OrderLog` (confirmed by reading lines 256-304 — no re-sort or buffering happens between `Tick()` and `AppendContactTransition`). Given the round-1 finding that `ApplyTargetKill`'s equivalent ordering bug transitively reached `BalticReplayHarness.Run`, this method sits in the same blast radius. Risk assessed as MEDIUM (same replay-critical path, narrower trigger condition — requires 2+ contacts to cross the stale threshold on the identical tick — but real and not contrived).
- **Regression sweep (before -> after fix, same commit)**:
  - `ProjectAegis.Sim.Tests`: 285/285 -> 286/286 (285 pre-existing + 1 new)
  - `ProjectAegis.Delegation.Tests`: 253/253 -> 253/253 (unaffected, unchanged)
  - `ProjectAegis.Delegation.UnityAdapter.Tests` (full suite, incl. Baltic golden-replay tests): 261/261 -> 261/261 (unaffected, unchanged — confirms no pinned Baltic golden/hash was touched)
  - `ProjectAegis.Data.Tests`: 477/477 -> 477/477 (unaffected — no dependency on `ProjectAegis.Sim`)
  - `ProjectAegis.Data.Excel.Tests`: 5/5 -> 5/5 (unaffected)
  - `ProjectAegis.MissionEditor.Cli.Tests`: not re-run this pass — grepped for any reference to `PdDetectionContactSimulator`/`EmitStaleLosses` in `src/ProjectAegis.MissionEditor.Cli` and `src/ProjectAegis.MissionEditor.Cli.Tests`; zero matches, so this project has no code path through the changed method and was skipped to stay within the sandbox time budget (baseline was 108/108 per session brief; nothing here would change that).

## Related Issues
- Sibling to `BUG-KILL-ORDER-0001` (`production/qa/bugs/BUG-kill-transition-order-nondeterminism.md`) — same root-cause class (unsorted `Dictionary<string, T>` enumeration feeding an order-log-visible transition list), same class (`PdDetectionContactSimulator`), but a different method/trigger (stale-timeout loss vs. combat-kill loss) that the prior fix did not touch or audit.
- Distinct from `BUG-fingerprint-negative-zero` — that bug is a floating-point sign-of-zero formatting hazard in `FingerprintFloat`; this one is a collection-ordering hazard in the sensor/contact-lifecycle layer. No file overlap.

## Notes
- This is the third method in `PdDetectionContactSimulator` responsible for emitting contact-lost transitions (`ApplyTargetBdaLost`, `ApplyTargetKill`, `EmitStaleLosses`); the first two already sorted ordinally, only the third did not. Recommend a follow-up audit confirming no other lifecycle-transition emitters in this class (or similar classes, e.g. `BdaContactLifecycleHotTickApplier`, `CatalogDamageHotTickApplier`) were missed — those two were spot-checked during this session and were both found to already iterate pre-sorted collections (`sortedTargetIds`, `ledger.GetSortedPlatformIds()`), so they are not affected.
- Fix is a pure ordering change (same lookup, same mutation, same emitted `ContactTransition` values) — a no-op for the common case of at most one contact going stale per tick, and only changes output ordering when 2+ contacts cross the stale threshold on the identical tick.
