# Bug Report

## Summary
**Title**: Phantom undo snapshot pushed to disk-backed undo stack when a mission add/update/delete CLI command rejects the mutation
**ID**: BUG-phantom-undo-snapshot-on-rejected-mission-mutation
**Severity**: S2-Major
**Priority**: P2-Next Sprint
**Status**: Open (fix in this commit; awaiting `/bug-report verify` once merged)
**Reported**: 2026-07-06
**Reporter**: gameplay-qa-agent (qa-loop-04-scenario-mission)

## Classification
- **Category**: Gameplay / Data integrity (scenario authoring / mission editor CLI)
- **System**: `ScenarioDocumentEditor` undo stack (`ScenarioUndoStackStore`), consumed by all Mission Add/Update/Delete CLI commands
- **Frequency**: Always, on every rejected mutation attempt (duplicate mission id on Add; mission-not-found or wrong-type on Update/Delete)
- **Regression**: Unknown/long-standing — the unconditional `PushUndoSnapshot` call predates this QA loop; not a new regression introduced by a recent change

## Environment
- **Build**: branch `qa-loop-04-scenario-mission`, branched from `main` @ `5eab203`
- **Platform**: headless .NET 8 layer (`ProjectAegis.MissionEditor.Cli`), no Unity Editor involved
- **Scene/Level**: N/A (CLI/editor-tooling bug, not in-game)
- **Game State**: Any scenario file being edited via the Mission Editor CLI (`mission add-*`, `mission update-*`, `mission delete`)

## Reproduction Steps
**Preconditions**: A valid scenario JSON file with at least one existing mission.

1. Run `mission add-patrol` (or any add command) successfully once. Undo stack now has 1 entry.
2. Run `mission delete` (or `mission update-*`) with a mission id that does **not** exist.
3. Observe the command correctly returns exit code 1 with `MISSION_NOT_FOUND` and the on-disk scenario file is untouched.
4. Inspect the sidecar undo stack file (`<scenario>.undo-stack.json`) / call `ScenarioUndoStackStore.Count(path)`.

**Expected Result**: Undo stack count remains 1 — a rejected mutation that never touched the scenario should not consume/alter undo history.
**Actual Result**: Undo stack count becomes 2 — a spurious ("phantom") snapshot was pushed for a mutation that never happened, silently desyncing `remainingUndoDepth` from real edit history. A subsequent `scenario undo` would restore to a state that is functionally identical to the one before it (since nothing changed), silently burning one legitimate undo level and potentially confusing users who expect N clean undos for N successful edits.

## Technical Context
- **Root cause**: All 8 mutating commands called `editor.PushUndoSnapshot(scenarioPath)` — which writes straight to the disk-backed undo stack — **before** attempting the mutation that can still fail (`AddPatrolMission`/`AddStrikeMission`/`AddFerryMission`/`AddSupportMission` throw `InvalidOperationException` on duplicate id; `UpdatePatrolMission`/`UpdateStrikeMission`/`UpdateFerryMission` throw via `RequireMission` on not-found/wrong-type; `MissionDeleteCommand` calls `TryRemoveMission` which returns `false` on not-found). When the mutation fails, the snapshot has already been persisted to disk, corrupting undo history.
- **Likely affected files** (production):
  - `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs` — added `CaptureUndoSnapshot()` (in-memory only, deep-copies `Missions` to avoid aliasing the live list) and `PersistUndoSnapshot(path, snapshot)` (writes to disk); `PushUndoSnapshot` retained for callers whose mutation genuinely cannot fail once reached, with an XML-doc warning against reuse in fallible call sites.
  - `src/ProjectAegis.MissionEditor.Cli/MissionAddPatrolCommand.cs`, `MissionAddStrikeCommand.cs`, `MissionAddFerryCommand.cs`, `MissionAddSupportCommand.cs`, `MissionUpdatePatrolCommand.cs`, `MissionUpdateStrikeCommand.cs`, `MissionUpdateFerryCommand.cs`, `MissionDeleteCommand.cs` — all switched from `PushUndoSnapshot` (before mutation) to `CaptureUndoSnapshot()` (before mutation, in-memory) + `PersistUndoSnapshot()` (only after the mutation call site returns/succeeds, i.e. after any early-return error path).
- **Related systems**: `ScenarioUndoStackStore` (disk-backed undo stack sidecar file), `ScenarioUndoCommand` (`scenario undo`) which pops from this same stack.
- **Possible root cause**: see above — snapshot-then-mutate ordering with no rollback on mutation failure.

## Evidence
- New regression test: `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs::scenario_undo_does_not_push_snapshot_on_mission_not_found_rejected_delete`
  - **Red (bug reproduced)**: with production files reverted to their pre-fix state (test kept), the test fails: `Assert.Equal() Failure: Expected: 1, Actual: 2`.
  - **Green (fix applied)**: full `ScenarioUndoCliTests` class passes 5/5.
- Impact/caller check (GitNexus CLI not reachable from this worktree — `.gitnexus/` index exists only in the main repo working tree, not in this git worktree checkout, and `node .gitnexus/run.cjs status` failed with a pnpm option error from the main repo root; fell back to manual `grep`-based caller analysis):
  - `CaptureUndoSnapshot` / `PersistUndoSnapshot` are called from exactly 8 production call sites, all in `src/ProjectAegis.MissionEditor.Cli/`: `MissionAddFerryCommand`, `MissionAddPatrolCommand`, `MissionAddStrikeCommand`, `MissionAddSupportCommand`, `MissionDeleteCommand`, `MissionUpdateFerryCommand`, `MissionUpdatePatrolCommand`, `MissionUpdateStrikeCommand`. Grep confirms no other command file in that directory references `PushUndoSnapshot`/`CaptureUndoSnapshot`/`PersistUndoSnapshot`, so the diff's blast radius is exactly the fallible mutation commands — nothing extra was touched and nothing fallible was missed.
  - `PushUndoSnapshot` (the old unconditional method) is now unused in production code (only referenced in a test comment); it is intentionally retained on `ScenarioDocumentEditor` as documented API for any future caller whose mutation cannot fail once reached.
  - **Blast radius: HIGH** — `ScenarioDocumentEditor` is a shared authoring component used by all 8 mission mutation commands; a mistake in the shared `CaptureUndoSnapshot`/`PersistUndoSnapshot` pair (e.g. forgetting the `.ToList()` materialization noted in the XML doc) would silently corrupt undo history across every mission type and every command (add/update/delete), not just one. The fix was verified to correctly deep-copy `Missions` so the in-memory capture isn't aliased by the live list during the mutation attempt.
- Test run results (`dotnet test`, this worktree, .NET 8.0.422):
  - `ProjectAegis.Sim.Tests`: 281/281 passed (before and after — untouched by this change).
  - `ProjectAegis.MissionEditor.Cli.Tests`:
    - Before fix (production reverted via `git stash`, test file kept): `ScenarioUndoCliTests` 4/5 passed (1 expected failure = the new regression test, proving red state).
    - After fix (all changes restored): `ScenarioUndoCliTests` 5/5 passed. Full project run (excluding one pre-existing, unrelated, environment-specific failure — see Notes): 71/71 passed.

## Related Issues
- None filed prior to this loop.

## Notes
- One test in this project, `BranchIntegrationPhase0SmokeTests.Phase0_smoke_script_exists_and_passes_quick_mode`, fails in this sandbox both with and without this diff applied (confirmed via `git stash`/`git stash pop` — the file is byte-identical to `main`). It shells out to a bash script (`RunBash`) that appears unavailable/non-executable in this sandboxed environment. This is a pre-existing, environment-specific failure unrelated to the scenario/mission undo bug fixed here, and is out of scope for this QA loop's focus area.
- GitNexus (`node .gitnexus/run.cjs ...`) could not be exercised from inside this git worktree: the `.gitnexus/` index directory exists only under the main repo working tree (`/sessions/great-loving-carson/mnt/cmano-clone/.gitnexus`), not inside `.worktrees/qa-loop-04-scenario-mission`, and invoking it from the main repo root errored (`Unknown option: 'allow-build'` from the underlying `pnpm dlx` invocation). Manual `grep`-based caller analysis was used instead per the fallback instruction in `CLAUDE.md`.
