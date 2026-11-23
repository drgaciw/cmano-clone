# Sprint 4 G3 Mission Editor Workflow Evidence (`DRG-274` / `[AUTH-01]`)

- **Date:** 2026-11-23 UTC
- **Branch:** `sprint/s4-h4-editors-g3`
- **Issues:** `DRG-274` (`[AUTH-01]`), verifying `DRG-346` (`[H4-FIRST-SLICE]`) and `DRG-371` (`[H4-CAPABILITY]`)
- **Rubric Reference:** `production/qa/dod-rubric-sprint-s4.md` (`H6`, `C4`, `C9`, `C11`, `C13`)
- **Unity Editor:** `6000.3.27f2` (`unity/ProjectAegis`, live MCP on `http://localhost:8081`, `DISPLAY=:0`)

---

## 1. Scope & Non-Rewriting Reuse Confirmation

This G3 evidence package verifies the end-to-end Mission Editor workflow across **shipped authoring surfaces** without rewriting Mission Editor core or introducing replacement hosts:

1. **Edit Mode Authoring Surface (`unity/ProjectAegis/Assets/Editor/ScenarioMapAuthoringWindow.cs`):**
   - Reuses the shipped UI Toolkit `ScenarioMapAuthoringPanel.uxml` / `.uss` host bound to `ScenarioAuthoringSession`, `ScenarioEditCommandBus`, `MapAuthoringSurface`, and `LiveFindingsPresenter`.
   - Exercised live against temporary `/tmp/s4_g3_probe/` copies of `bounded-authoring-supported.json`, `role-capability-generic-only.json`, `bounded-authoring-invalid.json`, and `bounded-authoring-unsupported-role.json`.
   - Exported validated artifacts via `ScenarioSaveExportGate.Export(...)` (`.export.json`) without altering any repository fixture.

2. **Runtime D26 Shell Host (`unity/ProjectAegis/Assets/Scripts/Runtime/ScenarioEditorShellHost.cs`):**
   - Reuses the shipped `ScenarioEditorShell.uxml` / `.uss` chrome and binds `D26` toolbar actions (`LoadRequested`, `SaveRequested`, `UndoRequested`, `RedoRequested`, `PlayRequested`, `SampleRequested`) to `BoundedAuthoringWorkflow` and `PlayEntrySession` (`ADR-010 §2-3`, `ADR-007`, `ADR-001`, `ADR-017` compliant; zero `ProjectAegis.Sim` imports).
   - Exercised live in Unity Play Mode via temporary runtime `S4_G3_TempShellHost` (`UIDocument` + `C2RuntimePanelSettings.asset` + `DelegationBridgeHost`), then destroyed cleanly before returning to Edit Mode without modifying or saving `DelegationSmoke.unity`.

3. **Headless Bounded Pipeline (`src/ProjectAegis.Delegation.UnityAdapter/Authoring/BoundedAuthoringWorkflow.cs`):**
   - Pure C# (`netstandard2.1`, zero `UnityEngine` references) coordinating `OpenDraft`, `SelectUnit`, `PlaceOrEditUnit`, `AssignUnitToMission`, `RemoveMission`, `Undo`/`Redo`, `Validate`, `SaveDraft` (ungated), `ExportPackage` (error-gated, non-blocking on `MISSION_ROLE_GENERIC_ONLY`), and `HandOffToPlay` (`PlayEntrySession` -> `SimulationPhase.Executing`).

4. **Hard-No Surfaces Untouched:** Zero edits to `DelegationBridge.cs` hotpath, `CatalogWriteGate` existing write paths, `SimulationSession.cs`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `DelegationSmoke.unity`, replay goldens, or `assets/data/catalog/*.db`.

---

## 2. Live Unity `:8081` Test Runner & Console Verification

### 2.1 Full Unity PlayMode Suite (`ProjectAegis.Unity.Tests`)

Executed sequentially via Unity `:8081` MCP `tests-run` (`testMode: "PlayMode"`, `testAssembly: "ProjectAegis.Unity.Tests"`):

- **Status:** `Passed` (`61 / 61` passed, `0` failed, `0` skipped)
- **Duration:** `00:00:05.3660460`
- **Unity Console (`LogEntries.GetCountsByType`):** `errors=0` (verified both after the PlayMode test run — `errors=0, warnings=3, logs=3, isPlaying=False` — and after exiting the live `ScenarioEditorShellHost` Play Mode session — `errors=0, warnings=1, logs=0, isPlaying=False`).

### 2.2 `Sprint4AuthoringPlayModeTests` (`8 / 8` Passed)

Executed via Unity `:8081` MCP `tests-run` (`testMode: "PlayMode"`, `testClass: "Sprint4AuthoringPlayModeTests"`, `Duration: 00:00:01.0608190`):

| # | Test Method | Status | Duration |
|---|---|---|---:|
| 1 | `ScenarioEditorShellHost_detach_reverts_all_six_d26_buttons_and_sample_exports_package` | Passed | `0.290s` |
| 2 | `ScenarioEditorShellHost_failed_open_preserves_default_draft_path_and_updates_status` | Passed | `0.056s` |
| 3 | `ScenarioEditorShellHost_mixed_capability_tiers_reports_errors_and_warnings_without_claiming_play_allowed` | Passed | `0.067s` |
| 4 | `ScenarioEditorShellHost_pre_awake_setters_do_not_throw` | Passed | `0.004s` |
| 5 | `ScenarioEditorShellHost_reenable_with_panel_asset_rewires_buttons_and_labels` | Passed | `0.095s` |
| 6 | `ScenarioEditorShellHost_supported_me_workflow_load_place_edit_save_undo_redo_reload_and_play_handoff` | Passed | `0.239s` |
| 7 | `ScenarioEditorShellHost_unattached_keeps_d26_not_available_gating` | Passed | `0.028s` |
| 8 | `ScenarioEditorShellHost_validation_blocks_play_allows_save_surfaces_generic_warnings_and_recovers` | Passed | `0.199s` |

### 2.3 Headless Authoring & Shell Contract Tests (`.NET 11`)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build \
  --filter "BoundedAuthoringWorkflowTests|ScenarioEditorShellHostContractTests" -v minimal
```

- **Result:** `Passed! - Failed: 0, Passed: 14, Skipped: 0, Total: 14, Duration: 231 ms`

---

## 3. Reproducible CLI / `.NET` Commands (Temp Fixture Copies)

All CLI checks below were executed on fresh copies under `/tmp/s4_g3_probe/cli/` (cleaned after verification; zero repository fixtures mutated).

### 3.1 Success Path & `cli-simulate-sample` Generic-Only Warnings (`DRG-371` / `DRG-346`)

```bash
export PATH="$HOME/.dotnet:$PATH"
mkdir -p /tmp/s4_g3_probe/cli
cp assets/data/scenarios/validation/bounded-authoring-supported.json /tmp/s4_g3_probe/cli/
cp assets/data/scenarios/validation/role-capability-generic-only.json /tmp/s4_g3_probe/cli/

# 1. Validate supported scenario -> passed=true, canExport=true, exit 0
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_validate --path /tmp/s4_g3_probe/cli/bounded-authoring-supported.json

# 2. Export validated package -> allowed=true, transforms=0, exit 0
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_export --path /tmp/s4_g3_probe/cli/bounded-authoring-supported.json

# 3. Run CLI simulate-sample on generic-only Patrol+Strike -> emits non-blocking MISSION_ROLE_GENERIC_ONLY warnings, exit 0
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_simulate_sample --path /tmp/s4_g3_probe/cli/role-capability-generic-only.json --ticks 5
```

Observed results:
- `scenario_validate`: `passed: true`, `canExport: true`, `reportHash: dfec4cd9904e9ac942d3163ca572c9ae0c94382abad8f8e04ec84fa32e7cdc6b`, `exit=0`.
- `scenario_export`: `ok: true`, `allowed: true`, `transformCount: 0`, `exit=0`.
- `scenario_simulate_sample`: `seed: 42`, `ticks: 5`, `engagementCount: 5`, emits `warnings` array containing two `MISSION_ROLE_GENERIC_ONLY` (`severity: "Warning"`) entries for `patrol-1` and `strike-1` on backend `cli-simulate-sample`, followed by `SEED=42 HASH=a717c33911b1d5ddd3b0e4c5c5feb587cdd26be9e6b7da0e6e7425999b819c4d` and `sample-complete`, `exit=0`.

### 3.2 Validation Blocked & Recovery Paths (`DRG-346`)

```bash
export PATH="$HOME/.dotnet:$PATH"
cp assets/data/scenarios/validation/bounded-authoring-invalid.json /tmp/s4_g3_probe/cli/
cp assets/data/scenarios/validation/bounded-authoring-unsupported-role.json /tmp/s4_g3_probe/cli/

# A. Invalid draft (unassigned patrol-1): blocked (MISSION_NO_UNITS, exit 1) -> assign u1 -> passes (exit 0)
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_validate --path /tmp/s4_g3_probe/cli/bounded-authoring-invalid.json

dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  mission_update_patrol --path /tmp/s4_g3_probe/cli/bounded-authoring-invalid.json --edit-version 1 --id patrol-1 --unit u1

dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_validate --path /tmp/s4_g3_probe/cli/bounded-authoring-invalid.json

# B. Unsupported Support roles (Tanker/AEW/EW): blocked (3x MISSION_ROLE_NOT_EXECUTED, exit 1) -> remove unsupported missions -> passes (exit 0)
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_validate --path /tmp/s4_g3_probe/cli/bounded-authoring-unsupported-role.json

dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  mission_delete --path /tmp/s4_g3_probe/cli/bounded-authoring-unsupported-role.json --edit-version 1 --id support-tanker
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  mission_delete --path /tmp/s4_g3_probe/cli/bounded-authoring-unsupported-role.json --edit-version 2 --id support-aew
dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  mission_delete --path /tmp/s4_g3_probe/cli/bounded-authoring-unsupported-role.json --edit-version 3 --id support-ew

dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- \
  scenario_validate --path /tmp/s4_g3_probe/cli/bounded-authoring-unsupported-role.json
```

Observed results:
- `bounded-authoring-invalid.json` pre-fix: `passed: false`, `canExport: false`, `MISSION_NO_UNITS` (`exit=1`). Post-fix (`editVersion: 2`): `passed: true`, `canExport: true`, `reportHash: dfec4cd9904e9ac942d3163ca572c9ae0c94382abad8f8e04ec84fa32e7cdc6b` (`exit=0`).
- `bounded-authoring-unsupported-role.json` pre-fix: `passed: false`, `canExport: false`, three `MISSION_ROLE_NOT_EXECUTED` errors (`support-aew`, `support-ew`, `support-tanker`), `exit=1`. Post-fix (`editVersion: 4`): `passed: true`, `canExport: true`, `exit=0`.

---

## 4. Live Unity Editor & Play Mode Screenshots (`:8081`)

### 4.1 Edit Mode `ScenarioMapAuthoringWindow` Captures

**Capture method:** Live Unity Editor `6000.3.27f2` on `DISPLAY=:0`; window `Scenario Map Authoring` (`X11 0x4c0118b`) driven sequentially via `:8081` MCP `script-execute` against temp copies under `/tmp/s4_g3_probe/`, then captured directly from the live X11 window via `DISPLAY=:0 import -window 0x4c0118b ...`. Every PNG was inspected directly to confirm crisp, non-blank UI Toolkit rendering.

1. **Success path (`bounded-authoring-supported.json`):**
   - Opened temp copy, selected `u1`, placed `u2` (`blue`, `ffg` @ `57.3, 20.4`), edited `u2` (`ddg` @ `57.5, 20.6`), saved draft (`Edit version: 3`, `Dirty: no`), validated (`READY TO EXPORT — 0 errors, 0 warnings`), and exported `/tmp/s4_g3_probe/bounded-authoring-supported.export.json` (`Exported validated artifact (0 logged transforms).`).
   - ![ScenarioMapAuthoringWindow Success Path](s4-g3-me-editor-success.png)

2. **`DRG-371` Generic-Only Warning Tier (`role-capability-generic-only.json` on `cli-simulate-sample`):**
   - Opened temp copy and evaluated findings against `MissionRoleCapabilityManifest.CliSimulateBackendId` (`"cli-simulate-sample"`). Gate banner shows green `READY TO EXPORT (generic warnings) — 0 errors, 2 warnings` (`CanExport=True`, `exportAllowed=True`), and `FINDINGS CODES` lists both non-blocking `[WARN] MISSION_ROLE_GENERIC_ONLY` findings (`patrol-1`, `strike-1`) with explicit `(Export and Play remain allowed)` disclosure.
   - ![ScenarioMapAuthoringWindow Generic-Only Warnings](s4-g3-me-editor-generic-warnings.png)

3. **Validation Blocked & Draft Save Allowed (`bounded-authoring-invalid.json`):**
   - Opened temp copy (`patrol-1` has empty `assignedUnitIds`). Gate banner shows red `EXPORT BLOCKED — 1 error, 0 warnings` (`MISSION_NO_UNITS`), `ScenarioSaveExportGate.Export` is blocked (`Allowed=false`), while `Save` remains enabled (`saveEnabled=True`) and successfully saves the work-in-progress draft (`editVersion=1`).
   - ![ScenarioMapAuthoringWindow Blocked Invalid Draft](s4-g3-me-editor-blocked.png)

4. **Validation Recovery (`bounded-authoring-invalid.json` recovered):**
   - Assigned `u1` to `patrol-1`, saved (`Edit version: 2`, `Dirty: no`), and re-validated. Gate banner transitions to green `READY TO EXPORT — 0 errors, 0 warnings` and export succeeds (`0 logged transforms`).
   - ![ScenarioMapAuthoringWindow Recovered Draft](s4-g3-me-editor-recovered.png)

5. **Unsupported Support Roles Blocked & Recovered (`bounded-authoring-unsupported-role.json`):**
   - Opened temp copy containing `support-tanker`, `support-aew`, `support-ew`. Gate banner shows red `EXPORT BLOCKED — 3 errors, 0 warnings`, `FINDINGS CODES` surfaces three `[BLOCKING] MISSION_ROLE_NOT_EXECUTED` errors, and `Save` remains enabled. Removing the three unsupported Support missions recovered the draft to `READY TO EXPORT — 0 errors, 0 warnings` (`canExport=True`).
   - ![ScenarioMapAuthoringWindow Unsupported Roles Blocked](s4-g3-me-editor-unsupported-blocked.png)

### 4.2 Play Mode `ScenarioEditorShellHost` Captures

**Capture method:** Unity Editor `:8081` entered Play Mode (`EditorApplication.isPlaying = true`), spawned temporary `S4_G3_TempShellHost` (`UIDocument` + `C2RuntimePanelSettings.asset` + `ScenarioEditorShell.uxml` + `ScenarioEditorShell.uss` + `DelegationBridgeHost` + `ScenarioEditorShellHost`) against `/tmp/s4_g3_probe/play/` copies (never saving `DelegationSmoke.unity`), and captured each Game View frame (`1477x721`) directly via `:8081` MCP `screenshot-game-view` before destroying the temporary GameObject and returning cleanly to Edit Mode (`0` Console errors).

1. **Success Workflow (`bounded-authoring-supported.json`):**
   - Loaded draft, selected `u1`, placed/edited `u2`, and saved (`v3`, `saved`). Map summary displays all 3 units (`hostile-1`, `u1`, `u2`) and `Selection: u1`; inspector shows `Selected: u1`; D26 toolbar buttons `Load`, `Save`, `Undo`, `Play`, and `Sample` are enabled.
   - ![ScenarioEditorShellHost Play Mode Success](s4-g3-shell-playmode-success.png)

2. **Validation Blocked (`bounded-authoring-unsupported-role.json` + invalid draft check):**
   - Verified `bounded-authoring-invalid.json` blocks Play (`1` error) while Save succeeds, then loaded `bounded-authoring-unsupported-role.json` and saved (`Draft saved with 3 blocking findings — not exported. Export stays blocked until resolved.`). Tab bar displays red `Blocked by 3 error findings`, `Play` and `Sample` buttons are disabled (`playEn=False`), `Save` remains enabled (`saveEn=True`), map slot shows `Capability: 3 unsupported role error(s) — Export and Play blocked.`, and the findings dock lists all 3 `[ERROR] MISSION_ROLE_NOT_EXECUTED` findings.
   - ![ScenarioEditorShellHost Play Mode Blocked](s4-g3-shell-playmode-blocked.png)

3. **Recovered + Play Handoff (`Executing`):**
   - Removed `support-tanker`, `support-aew`, and `support-ew`, saved (`v4`, `saved`), verified `0` errors and `Play` re-enabled (`playEn=True`), and executed `ExportAndPlayAuthoring()`. Status bar and map slot report `Handed off 'bounded-authoring-unsupported-role.export.json' to Play (Mixed, Friendly).` with both `PlayEntrySession.State.Phase` and `DelegationBridgeHost.Bridge.Phase` at `SimulationPhase.Executing`.
   - ![ScenarioEditorShellHost Play Mode Recovered and Executing](s4-g3-shell-playmode-recovered-play.png)

---

## 5. Governance & Notion Rubric `H6` Note

- **H6 / C10 Owner Play Mode Walk Boundary:** The live Unity `:8081` test run (`61/61` PlayMode, `8/8` `Sprint4AuthoringPlayModeTests`, `0` Console errors), CLI verification, and 8 Editor/Game View screenshots in this package provide **agent/MCP prep evidence** for rubric gates `C4`, `C9`, `C11`, and `C13`. **Interactive owner Play Mode walk and final `H6` / `C10` sign-off remain owner-only.**
- **Tracker Honesty (`H2`):** Evidence comments only on Linear (`DRG-274`, `DRG-346`, `DRG-371`); issues are never marked `Done` prior to owner PR merge. `DRG-236` and `DRG-231` remain `hold:owner-go`.
