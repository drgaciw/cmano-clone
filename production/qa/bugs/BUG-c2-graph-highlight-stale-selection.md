# Bug Report

## Summary
**Title**: `C2PresentationController` leaves stale dependency-graph highlights/link-chain from a previously selected friendly unit visible after selection switches to a hostile contact
**ID**: BUG-C2GRAPH-0001
**Severity**: S3-Minor
**Priority**: P3-Backlog
**Status**: Open
**Reported**: 2026-07-05
**Reporter**: gameplay-qa-agent (qa-loop-08-unity-adapter)

## Classification
- **Category**: UI / Integration / Presentation (C2 view desync — editorState vs sim-state mismatch)
- **System**: C2 presentation/selection bridge — `ProjectAegis.Delegation.UnityAdapter.Presentation.C2PresentationController`, consumed by `DelegationBridgeHost` (Unity host) via `IC2PresentationFeed`
- **Frequency**: Always — deterministic, reproduces every time a friendly unit with graph surfacing applied is followed by a hostile-contact selection
- **Regression**: Unknown — the `ApplyGraphSurfacing` / `LastGraphHighlightIds` / `LastGraphLinkChainDisplay` feature was added for S37-04 graph surfacing; this stale-clear gap appears to have existed since that feature landed and was never covered by a test that switches selection *after* graph surfacing has been applied.

## Environment
- **Build**: branch `qa-loop-08-unity-adapter`, worktree HEAD at repo commit matching `main` at loop start
- **Platform**: linux, .NET 8.0 SDK, `dotnet test` (no Unity Editor available in this sandbox — bug and fix are entirely in the plain .NET `ProjectAegis.Delegation.UnityAdapter` assembly; the Unity-only consumer `DelegationBridgeHost.cs` is not compiled here but was read for context, under `#if UNITY_5_3_OR_NEWER`)
- **Scene/Level**: N/A — pure presentation-bridge bug, exercised via `ProjectAegis.Delegation.UnityAdapter.Tests`
- **Game State**: Player has a friendly unit selected in the C2 left drawer/dependency-graph panel, graph surfacing has been computed for it (highlights + link-chain populated), then the player selects a hostile contact instead (e.g. clicking a contact in the sensor picture)

## Reproduction Steps
**Preconditions**: A `C2PresentationController` with a friendly unit selected and `ApplyGraphSurfacing` already invoked for that unit (as `DelegationBridgeHost.SelectUnit` does via `ApplyGraphSurfacingForSelection`).

1. `controller.SelectFriendlyUnit("u1")`
2. `controller.ApplyGraphSurfacing(catalogReader)` → populates `LastGraphHighlightIds` (e.g. `["u1", "radar-1", "radar-2"]`) and `LastGraphLinkChainDisplay`
3. `controller.SelectHostileContact("c1", contacts)` — this is exactly what `DelegationBridgeHost.SelectContact` calls; note that host method deliberately does **not** call `ApplyGraphSurfacingForSelection` afterward, with the comment `// contacts do not drive platform graph`
4. Inspect `controller.LastGraphHighlightIds` / `controller.LastGraphLinkChainDisplay`

**Expected Result**: Once selection moves to a hostile contact (which has no platform dependency graph), the previous friendly unit's graph highlights and link-chain display should be cleared — `LastGraphHighlightIds` empty, `LastGraphLinkChainDisplay` null.

**Actual Result** (pre-fix): `LastGraphHighlightIds` still contained `["u1", "radar-1", "radar-2"]` and `LastGraphLinkChainDisplay` still held the previous unit's chain string. Because `DelegationBridgeHost.SelectContact` never recomputes graph surfacing for contacts, this stale state is **not transient** — it persists indefinitely until the player selects a different friendly unit. A C2 dependency-graph panel bound to these properties would keep highlighting equipment belonging to a unit that is no longer selected while the rest of the UI (unit detail panel, OOB tree, contact summary) correctly shows the hostile contact as the active selection.

## Technical Context
- **Likely affected files**:
  - `src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs` — `SelectFriendlyUnit` and `SelectHostileContact` did not clear `LastGraphHighlightIds` / `LastGraphLinkChainDisplay` on selection change (only `ApplyGraphSurfacing` and `ClearGraphSurfacing` touched those fields)
- **Related systems**:
  - `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` — `SelectContact` (line ~138-144) intentionally skips `ApplyGraphSurfacingForSelection()` for contacts; `SelectUnit` (line ~131-136) does call it synchronously right after selecting, which is why the friendly-unit-to-friendly-unit switch case was not independently visibly stale in that specific host wiring — but the underlying controller state was still logically stale for any other consumer of `C2PresentationController` (or future callers of `SelectFriendlyUnit`/`SelectHostileContact` that don't immediately re-run graph surfacing).
  - `src/ProjectAegis.Delegation.UnityAdapter/Bridge/IC2PresentationFeed.cs` — exposes `LastGraphHighlightIds`/`LastGraphLinkChainDisplay` as the read-only contract Unity panel hosts bind to.
- **Possible root cause**: `ApplyGraphSurfacing`/`ClearGraphSurfacing` were added in S37-04 as a separate, explicitly-invoked step from selection, and the selection mutators (`SelectFriendlyUnit`, `SelectHostileContact`) were never updated to invalidate the previously computed graph surfacing snapshot when selection changes. This is the same "shared-state-not-invalidated-on-transition" pattern independent of whether the caller remembers to re-run `ApplyGraphSurfacing`.

## Evidence
- **New failing test (red)**: `ProjectAegis.Delegation.UnityAdapter.Tests.Presentation.C2PresentationControllerTests.SelectHostileContact_clears_stale_graph_highlights_from_previous_unit`
  - Path: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/C2PresentationControllerTests.cs`
  - Failure before fix:
    ```
    Assert.That(controller.LastGraphHighlightIds, Is.Empty)
      Expected: <empty>
      But was:  < "u1", "radar-1", "radar-2" >
    ```
- **Fix**: `SelectFriendlyUnit` now calls `ClearGraphSurfacing()` when the newly selected unit id differs from the currently selected one; `SelectHostileContact` unconditionally calls `ClearGraphSurfacing()` before assigning the new contact selection (contacts never have a platform graph, so clearing is always correct there).
- **Post-fix (green)**: same test passes.
  - `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests` → **261/261** passed (baseline 260/260 + 1 new test), zero regressions.
  - `dotnet test src/ProjectAegis.Delegation.Tests` → **251/251** unchanged (this project does not reference `C2PresentationController`).

## Related Issues
- None filed prior to this session.

## Notes
- Impact analysis performed manually (GitNexus index not reachable inside this isolated worktree — `node .gitnexus/run.cjs analyze` found no `.gitnexus/` directory present; noting this explicitly per repo policy). Grep-based caller search for `SelectFriendlyUnit`/`SelectHostileContact` found exactly two production call sites, both in `unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs` (`SelectUnit`, `SelectContact`), plus the existing/updated unit tests. Both host call sites only ever pass through the controller's public selection API and read the resulting `LastGraphHighlightIds`/`LastGraphLinkChainDisplay` afterward, so clearing stale state earlier can only make previously-incorrect (stale) reads correct — it cannot make a previously-correct read incorrect. Risk assessed as LOW: the change is additive validation state clearing, all existing suites remain green, and the Unity host file (guarded by `#if UNITY_5_3_OR_NEWER`) is unaffected because it never asserted on the previously-stale values.
- Suggested follow-up: consider whether `DelegationBridgeHost.SelectContact`'s comment "contacts do not drive platform graph" should be updated/expanded now that the controller itself guarantees the clear, to make it clear the host no longer needs (and never needed) to call `ApplyGraphSurfacingForSelection` for the clearing behavior — it was already relying on an assumption the controller didn't actually uphold.
