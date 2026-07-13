# Bug Report

## Summary
**Title**: `scenario undo` silently wipes Sides/Features/Orbat/ReferencePoints/OperationsTimeline/Events/Variables/EditorState (and per-mission StationGeometry/EmconOverride) instead of restoring them
**ID**: BUG-undo-snapshot-drops-untouched-canonical-sections
**Severity**: S1-Critical
**Priority**: P1-Immediate
**Status**: Open (fix in this commit; awaiting `/bug-report verify` once merged)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-r2-04-scenario-mission)

## Classification
- **Category**: Gameplay / Data integrity (scenario authoring / mission editor CLI, undo system)
- **System**: `ScenarioUndoStackStore` (disk-backed undo stack sidecar), consumed by every mission add/update/delete CLI command and `scenario undo`
- **Frequency**: Always — every successful `scenario undo` on any scenario that has non-mission canonical data (sides, features, orbat, reference points, operations timeline, events, variables, editor state) or a Support mission with `StationGeometry`/`EmconOverride` set
- **Regression**: Unknown/long-standing — `CloneDocument`'s hand-copied field subset predates this QA loop and was not touched by the round-1 phantom-undo-snapshot fix (`BUG-phantom-undo-snapshot-on-rejected-mission-mutation.md`), which only changed *when* a snapshot is pushed, not *what* it contains.

## Environment
- **Build**: branch `qa-r2-04-scenario-mission`, branched from `main` @ `e2e1342`
- **Platform**: headless .NET 8 layer (`ProjectAegis.Data`, `ProjectAegis.MissionEditor.Cli`), no Unity Editor involved
- **Scene/Level**: N/A (CLI/editor-tooling bug, not in-game)
- **Game State**: Any scenario file being edited via the Mission Editor CLI that has populated `sides`, `features`, `orbat`, `referencePoints`, `operationsTimeline`, `events`, `variables`, `editorState`, or a Support mission with `stationGeometry`/`emconOverride`

## Reproduction Steps
**Preconditions**: A scenario JSON file with at least one `Side` entry and one existing Support mission whose `StationGeometry`/`EmconOverride` are populated.

1. Run any mutating mission command that succeeds (e.g. `mission add-patrol`). This calls `ScenarioDocumentEditor.CaptureUndoSnapshot()` before the mutation and `PersistUndoSnapshot()` after it succeeds, which calls `ScenarioUndoStackStore.Push`.
2. Run `scenario undo`. This calls `ScenarioDocumentEditor.PopUndo` → `ScenarioUndoStackStore.TryPop` → `RestoreFromDto`.
3. Inspect the scenario file on disk after the undo completes.

**Expected Result**: The added mission is reverted (correct today), and every section that was *not* part of the mutation — `Sides`, `Features`, `Orbat`, `ReferencePoints`, `OperationsTimeline`, `Events`, `Variables`, `EditorState`, and untouched missions' `StationGeometry`/`EmconOverride` — is restored exactly as it was before the mutation.
**Actual Result**: The mission revert works, but every other section listed above is wiped to its empty/null default. A scenario with a defined "Blue Force" side and a tanker support mission loses the side entirely and the tanker mission's station geometry/EMCON override after a single `scenario undo` — even though neither was ever touched by the mutation that was undone.

## Technical Context
- **Root cause**: `ScenarioUndoStackStore.CloneDocument` (`src/ProjectAegis.Data/Scenario/Authoring/ScenarioUndoStackStore.cs`), used by both `Push` (before writing the on-disk undo sidecar) and `TryPop` (after reading it back), constructed a new `ScenarioDocumentDto` by hand, copying only `Metadata` and a partial `Missions` projection (`Id`, `Type`, `AssignedUnitIds`, `TargetIds`, `FerryDestinationBaseId`, `PatrolZone`, `SupportRole`, `RoeOverride`). Every other `ScenarioDocumentDto` field — `Features`, `Sides`, `Orbat`, `ReferencePoints`, `OperationsTimeline`, `Events`, `Variables`, `EditorState` — and two `ScenarioMissionDto` fields — `StationGeometry`, `EmconOverride` — were omitted from the object initializer, so they silently fall back to their type defaults (`null`/empty array) both when the snapshot is written to the sidecar file and again when it is read back and restored. This function was evidently never updated as new DTO fields (Sides, Orbat, Events, StationGeometry, EmconOverride, etc.) were added over time.
- **Likely affected files** (production):
  - `src/ProjectAegis.Data/Scenario/Authoring/ScenarioUndoStackStore.cs` — `CloneDocument` now copies every `ScenarioDocumentDto`/`ScenarioMissionDto` field, with defensive copies for mutable collections (`Missions`, `Events`, `Variables`, `EditorState`) and reference sharing for the already-immutable/read-only sections (`Features`, `Sides`, `Orbat`, `ReferencePoints`, `OperationsTimeline`), matching the existing convention used elsewhere in `ScenarioDocumentEditor.RestoreCanonicalSections`.
  - No CLI command files needed to change — the fix is fully contained in the shared clone helper used by `Push`/`TryPop`.
- **Related systems**: `ScenarioDocumentEditor.PersistUndoSnapshot`/`PopUndo`/`RestoreFromDto` (calls into `ScenarioUndoStackStore`), all 8 mission add/update/delete CLI commands (indirect callers via `PersistUndoSnapshot`), `ScenarioUndoCommand` (`scenario undo`, direct caller via `PopUndo`).
- **Possible root cause**: see above — an incomplete, hand-maintained field-by-field clone that was never kept in sync with `ScenarioDocumentDto`/`ScenarioMissionDto` as they grew new fields.

## Evidence
- New regression test: `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs::scenario_undo_preserves_sides_and_support_mission_fields_not_touched_by_the_mutation`
  - **Red (bug reproduced)**: with `ScenarioUndoStackStore.CloneDocument` reverted to its pre-fix state (test kept), the test fails at `Assert.Single(dto.Sides)` — `Assert.Single() Failure: The collection was empty` — proving the seeded `blue` side is wiped by the undo round trip.
  - **Green (fix applied)**: full `ScenarioUndoCliTests` class passes 6/6 (5 pre-existing + 1 new).
- Impact/caller check (GitNexus CLI not reachable from this isolated worktree per `CLAUDE.md`'s documented fallback; used manual `grep`-based caller analysis instead):
  - `ScenarioUndoStackStore.Push` is called from exactly two production sites: `ScenarioDocumentEditor.PushUndoSnapshot` (retained but unused in production per the round-1 fix notes) and `ScenarioDocumentEditor.PersistUndoSnapshot` (used by all 8 mission mutation commands: `MissionAddPatrolCommand`, `MissionAddStrikeCommand`, `MissionAddFerryCommand`, `MissionAddSupportCommand`, `MissionUpdatePatrolCommand`, `MissionUpdateStrikeCommand`, `MissionUpdateFerryCommand`, `MissionDeleteCommand`).
  - `ScenarioUndoStackStore.TryPop` is called from exactly one production site: `ScenarioDocumentEditor.PopUndo`, called only by `ScenarioUndoCommand.Run` (`scenario undo`).
  - `CloneDocument` itself is private to `ScenarioUndoStackStore.cs` and has no other callers — the fix's blast radius is precisely bounded to the shape of every undo snapshot ever written to or read from the `.undo-stack.json` sidecar.
  - **Blast radius: HIGH** — `ScenarioUndoStackStore` is the single shared serialization boundary for the disk-backed undo stack used by every one of the 8 mission mutation commands plus `scenario undo` itself (the same shared-component class of risk flagged in the round-1 report for `ScenarioDocumentEditor`). Any scenario with sides/features/orbat/reference points/operations timeline/events/variables/editor-state data, or any Support mission with station geometry or an EMCON override, silently lost that data on every successful undo prior to this fix. The fix is additive only (new fields copied that weren't copied before); existing `.undo-stack.json` sidecar files missing these fields deserialize fine since `System.Text.Json` treats absent properties as their default, so there is no backward-compatibility break for already-written sidecar files.
- Test run results (`dotnet test`, this worktree, .NET 8.0.422):
  - `ProjectAegis.Sim.Tests`: 285/285 passed (before and after — untouched by this change).
  - `ProjectAegis.Data.Tests` (scoped `Scenario|Undo|Event` filter, since the changed file lives in `ProjectAegis.Data`): 73/73 passed (before and after).
  - `ProjectAegis.MissionEditor.Cli.Tests`:
    - `ScenarioUndoCliTests` alone: before fix 5/5 passed + 1 new test failing (red, 6 total); after fix 6/6 passed (green).
    - Full project regression sweep, run in two batches to stay under the sandbox's 45s-per-call limit: 62/62 passed (all classes except `ScenarioSimulateSampleCliTests` and `BranchIntegrationPhase0SmokeTests`) + 11/11 passed (`ScenarioSimulateSampleCliTests`) = **73/73 passed**, both before and after the fix (the only test whose outcome changed is the new one in `ScenarioUndoCliTests`, red→green as described above).
    - `BranchIntegrationPhase0SmokeTests.Phase0_smoke_script_exists_and_passes_quick_mode` could not be executed in this sandbox — it shells out via `RunBash` and the invocation hangs past the tool's 45-second timeout, exactly as documented as a pre-existing, environment-specific, out-of-scope issue in the round-1 bug report (`BUG-phantom-undo-snapshot-on-rejected-mission-mutation.md`). It is unrelated to this fix (the file was not touched) and was not counted in the totals above.

## Related Issues
- Distinct from `BUG-phantom-undo-snapshot-on-rejected-mission-mutation.md` (round 1): that bug was about a spurious snapshot being *pushed* on a rejected mutation (timing of `Push`). This bug is about *what* a legitimately pushed/popped snapshot actually contains — the shared clone helper never captured most of the document, independent of when it is called.

## Notes
- GitNexus (`node .gitnexus/run.cjs ...`) could not be exercised from inside this git worktree, consistent with the round-1 report: the `.gitnexus/` index exists only under the main repo working tree, not inside `.worktrees/qa-r2-04-scenario-mission`. Manual `grep`-based caller analysis was used instead per the fallback instruction in `CLAUDE.md`.
- The stated baseline for `ProjectAegis.MissionEditor.Cli.Tests` in this loop's task briefing was 108/108; `dotnet test --list-tests` against this worktree shows only 74 discoverable tests (73 excluding the known-hanging `BranchIntegrationPhase0SmokeTests` case) both before and after this change. This discrepancy predates and is unrelated to this fix — it was not introduced by any file touched here (only `ScenarioUndoStackStore.cs` and `ScenarioUndoCliTests.cs` were modified) — but is noted here transparently since it could not be reconciled within this loop's scope.
