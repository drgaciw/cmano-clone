# Plan — UAT round 5: implement Jev's decisions (2026-11-21)

**Spec:** `production/qa/uat-dod-rubric.md` (Definition of Done) and the Jev decision record `production/qa/uat-jev-round5-decisions.md`. Defect details: `production/qa/uat-ui-function-matrix.md`.
**Branch:** `uat/round5-jev-decisions` (stacked on `uat/ui-function-matrix-r1`, PR #906).

## Global Constraints

- TDD: every code change starts with a NUnit test that FAILS on the current code for the stated reason (record the failure message), then passes. Headless tests live in `src/*Tests`; run with `export PATH="$HOME/.dotnet:$PATH"; dotnet test <project> -v q`.
- `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` must stay byte-identical to `main` (zero-touch invariant, checked by `tools/ci/smoke-scenario-editor-phase0.sh --quick`). Do not edit `CatalogWriteGate` or replay goldens.
- Never commit `assets/data/catalog/*.db`; if a test run modifies them, `git checkout -- assets/data/catalog` before committing.
- Do not open or drive the Unity Editor (the controller owns it). Unity MonoBehaviour hosts under `unity/ProjectAegis/Assets/Scripts/Runtime/` and Editor scripts under `Assets/Editor/` are not compiled headlessly: put decisions in testable code (UnityAdapter/Delegation projections or pure helpers) or guard with source-text tests, the pattern used by `src/ProjectAegis.Delegation.UnityAdapter.Tests/Packaging/*Tests.cs`.
- Unity code is C# 10 (`Assets/csc.rsp`); headless code is C# 12, `netstandard2.1` for shared libraries.
- One commit per task, Conventional Commits, body references the UAT ID, ends with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Do not push.
- Do not dispatch subagents.

### Task 1: D26 — disable unwired in-game Scenario Editor actions with a reason

Jev decision: `disable_with_reason`. `unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs` wires Load/Save/Undo/Redo/Play/Sample buttons to events (`LoadRequested`, `SaveRequested`, `UndoRequested`, `RedoRequested`, `PlayRequested`, `SampleRequested`) that nothing subscribes to. Today Load is enabled and does nothing.

Acceptance:
- An action button whose event has no subscriber is disabled and its tooltip reads `NOT_AVAILABLE: <action> is not wired in this build` (exact prefix `NOT_AVAILABLE:`). When a subscriber exists, existing enable rules apply unchanged.
- The decision lives in a pure, headlessly tested helper (e.g. in `src/ProjectAegis.Delegation.UnityAdapter/Authoring/` or `Presentation/`, wherever the shell's state model lives — find `ScenarioEditorShell` state types) taking "has subscriber" + "state says enabled" and returning enabled/tooltip. The host calls it on refresh.
- Tests: helper truth table (no subscriber ⇒ disabled + NOT_AVAILABLE tooltip; subscriber + state enabled ⇒ enabled; subscriber + state disabled ⇒ disabled with existing reason).

### Task 2: D24b — contact-panel range-assessment hint matches exclusive selection

Jev decision: `change_hint_text_only`. Selection is exclusive by design (`C2PresentationController.SelectHostileContact` clears the unit). `src/ProjectAegis.Delegation.UnityAdapter/Presentation/DlzLiveSurfaceBinder.cs` (~line 124) emits `DLZ: UNKNOWN — select a friendly unit for range assessment`, which can never be satisfied while a contact is selected.

Acceptance:
- The contact-panel text becomes exactly `DLZ: UNKNOWN — range assessment is shown on the selected unit's panel`.
- Update every test/assertion that pins the old string (grep `select a friendly unit for range assessment` across `src/` and `unity/ProjectAegis/Assets/Tests/`). Unit-panel DLZ behaviour is unchanged.
- RED: a test asserting the new string fails on the current code.

### Task 3: D28 — "Play Baltic Escort" plays the committed scene without rebuilding

Jev decision: `play_without_rebuild`. `unity/ProjectAegis/Assets/Editor/BalticEscortSceneBuilder.cs` menu `Project Aegis/Play Baltic Escort` calls `Build()` (which re-creates and saves `Assets/Scenes/BalticEscort.unity` with new file IDs) before entering Play Mode, leaving the committed scene modified in git on every run.

Acceptance:
- Play opens `Assets/Scenes/BalticEscort.unity` and enters Play Mode; it calls `Build()` only when that scene file does not exist. `Project Aegis/Build Baltic Escort Scene` still rebuilds.
- Keep `SaveCurrentModifiedScenesIfUserWantsTo` behaviour.
- Guard test (source-text, under `src/ProjectAegis.Delegation.UnityAdapter.Tests/Packaging/`): the Play menu method only calls `Build()` inside a scene-file-existence check, and opens the scene via `EditorSceneManager.OpenScene`. RED on current code.

### Task 4: D35 — commit the Addressables settings for the APP-6 atlas

Jev decision: `commit_generated_settings`. No `unity/ProjectAegis/Assets/AddressableAssetsData/` is committed. The controller ran `Project Aegis/Addressables/Ensure App6 MapPresentation Group` in the Editor; Unity generated 28 files, saved at `/tmp/claude-1000/-home-username01-cmano-clone/55dbfae9-0dbe-4a7c-9930-ac7f1acd4bdc/scratchpad/addressables-generated/` (contains `AddressableAssetsData/` and `AddressableAssetsData.meta`).

Acceptance:
- Copy that folder and its `.meta` into `unity/ProjectAegis/Assets/` unchanged and commit them.
- Guard test under `Packaging/`: `Assets/AddressableAssetsData/AddressableAssetSettings.asset` exists, and some file under `Assets/AddressableAssetsData/AssetGroups/` references the GUID found in `Assets/UI/MapPlaceholder/App6FrameAtlas.png.meta` and the address `Map/App6FrameAtlas`. RED before the copy (file missing). Every new `.meta` must pass the existing 32-hex GUID test (`UnityMetaGuidValidityTests`).
- Check `.gitignore` does not exclude the folder; if it does, report instead of changing `.gitignore`.

### Task 5: CLI high-value subset — reject invalid input instead of corrupting data

Jev decision: `fix_high_value_subset`. Mission Editor CLI (`src/ProjectAegis.MissionEditor.Cli`), defects from `production/qa/evidence/uat-2026-11-21/mission-editor-cli-results.md`:
- CLI-7: authoring verbs accept and save invalid values: `reference_point_upsert --type bogus`, circle `--radius-nm -3`, circle with no radius, polygon with 2 vertices; `event_add --trigger Bogus` / `--condition Bogus`; `side_upsert --roe Bogus` / `--emcon Bogus`. Expected: error envelope `INVALID_*` (follow the existing `mission_update_support` `INVALID_ROLE` pattern), exit 1, file unchanged.
- CLI-12: read-only catalog verbs (`catalog_intelligence_run`, `catalog_platform_browse`, `catalog_dependency_graph`, `catalog_link_report`, `catalog_mount_loadout_quarantine_triage`) given a non-existent `--db` create a ~332 KB SQLite file and report ok. Expected: `NOT_FOUND` error envelope, exit 1, no file created. Only when `--db` is explicitly passed; default shipped-catalog resolution is unchanged.
- CLI-4: `scenario_event_trace --event <unknown id>` exits 0 with an empty trace; `--path missing.json` only warns. Expected: `NOT_FOUND` envelope, exit 1.
Acceptance: one parameterized test per defect group (RED first), existing CLI tests stay green (`dotnet test src/ProjectAegis.MissionEditor.Cli.Tests`). Missing-file `exit 2` behaviour of other verbs (CLI-2/3) is out of scope.

### Task 6: C8 — runtime red evidence for D24 and D25

The rubric needs each fix's test to fail on the pre-fix code. D24 (`8c90cbf6`-equivalent commit "disable attack options when no unit is selected") and D25 ("confirm successful toolbar orders with ISSUED status") only showed compile-time red because the helper did not exist.

Acceptance (evidence only, no production change):
- In a scratch worktree at the parent of each fix commit (find SHAs with `git log --oneline --grep "UAT D24"` / `--grep "UAT D25"` on this branch), add the fix commit's test file plus a minimal stub of the helper that reproduces the OLD behaviour (D24: gate returns the old fallback; D25: status formatter returns empty as `SetStatus(null)` did), run the test, and capture the runtime assertion failure.
- Write `production/qa/evidence/uat-2026-11-21/c8-red-evidence.md` with, per defect: fix commit SHA, parent SHA, test name, exact failure message, and the stub used. Commit only that file. Remove the scratch worktree afterwards.

### Task 7: D36 — clicking the red unit's own map marker opens an empty Contact Detail

Found in round 5 UAT (map-marker select, C9). Under FULL PICTURE fog the map shows three markers in `baltic-patrol-classify`: `u1` (friendly), `c1` (hostile contact tracking `hostile-1`) and `hostile-1` (the red unit itself). Clicking `u1` selects the unit and clicking `c1` opens Contact Detail for `c1` — both correct. Clicking `hostile-1` calls `C2SelectionResolver.TryResolveHostileContactFromSymbol("hostile-1", symbols, …)` (`src/ProjectAegis.Delegation/Projection/C2SelectionResolver.cs`, called from `MapPlaceholderPanelHost.OnSymbolClicked`) and selects contact id `hostile-1`, which is not a contact, so Contact Detail shows `CONTACT: —` / `TARGET: —`.

Acceptance:
- Clicking a hostile unit's own symbol selects the contact whose target is that unit (here `c1`, `TargetId = hostile-1`) when one exists; when no contact tracks it, nothing is selected (no empty Contact Detail).
- The mapping lives in `C2SelectionResolver` (headless) using the data the resolver already receives (map symbols) plus whatever contact list the caller can pass; if the signature must change, update the single caller in `MapPlaceholderPanelHost.OnSymbolClicked` (Unity-only, C# 10) to pass the contacts it already has access to.
- Tests in `src/ProjectAegis.Delegation.Tests` or `UnityAdapter.Tests` (wherever `C2SelectionResolver` tests live): RED — resolving `hostile-1` returns `hostile-1` today; GREEN — returns `c1`; no-tracking-contact case returns false.

### Task 8: D15 layout-safe subset — reposition the unit-order toolbar so it can join the scene

Jev decision for D15/D18: `add_layout_safe_subset`. Controller measurement (Play Mode, 1477x721 Game View, panel reference 2056x1004): of the 24 hosts missing from `DelegationSmoke.unity`, none is fully layout-safe today. Small HUDs (MapScaleHud, AxisControl, SensorC2, JevAttention) anchor at the top-left (0,0) over the top bar/drawer; side panels cover 2–100% of the map. `UnitOrderToolbarHost` covers 0.1% of the map but its box (`left: 50%; bottom: 48px` in `unity/ProjectAegis/Assets/UI/UnitOrderToolbar/UnitOrderToolbarPanel.uss`) overlaps the map's control-button row (Tactical / Operational / Theater / Follow selection / Earlier events / Latest events), which spans roughly the left 2/3 of the area under the map. Ruling: only the unit-order toolbar joins the scene this round (orders are core C9 functions); all other hosts stay backlog with the measured overlap recorded.

Acceptance (this task, files only — the controller adds the host to the scene in the Editor and measures):
- Change only `UnitOrderToolbarPanel.uss` positioning so the toolbar sits bottom-right of centre, clear of the control-button row and clear of the bottom-right Message Log (which is `right: 8px; bottom: 8px; max-height: 220px`, ~300 px wide in panel units — check `Assets/UI/MessageLog/*.uss` for the real values): e.g. anchor with `right:` instead of `left: 50%`, placing its right edge left of the Message Log with a gap, and its bottom aligned with the control-button row. Use tokens from `Assets/UI/AegisTokens.uss` where one exists (e.g. the `--c2-message-log-clearance` token added for D30) rather than magic numbers.
- Update `RequiredHosts` in `src/ProjectAegis.Delegation.UnityAdapter.Tests/Packaging/DelegationSmokeSceneHostCoverageTests.cs` to `{ "AttentionToastPanelHost", "UnitOrderToolbarHost" }` and its doc comment (UnitOrderToolbar added as the layout-safe subset of D15). This test goes RED until the controller adds the host to the scene — that is the expected RED; commit the USS + test change together with a note in the commit body that the scene change follows in a controller commit.
- Add a source-text guard test that `.unit-order-toolbar` no longer uses `left: 50%` (the overlap cause) and anchors with `right:`.
