# Sprint 1 C8 / H5 — Recorded RED→GREEN TDD & Source-Fence Evidence

Branch: `sprint/s1-hygiene-wiring` (2026-11-22)

## 1. DRG-772 (`MissionEditor.Cli → Ai.Host` Process Split)
- **RED (pre-fix `ProjectAegis.MissionEditor.Cli.csproj`):**
  - `CsprojReferenceGraphPinTests.MissionEditor_cli_does_not_reference_ai_host_or_ai_provider_packages`:
    `Expected: not some item equal to "ProjectAegis.Ai.Host.csproj" / But was: < "ProjectAegis.Ai.Host.csproj", ... >`
  - `AiHostProcessBoundaryTests.MissionEditor_cli_csproj_and_assembly_do_not_reference_ai_host_or_ms_extensions_ai`:
    `DRG-772: MissionEditor.Cli.csproj must not hold a ProjectReference to ProjectAegis.Ai.Host.csproj.`
- **GREEN:** `ProjectAegis.Ai.Contracts.Tests` 18/18, `ProjectAegis.Ai.Host.Tests` 79/79, `ProjectAegis.MissionEditor.Cli.Tests` 168/168, `CsprojReferenceGraphPinTests` + `AiHostBoundaryGateTests` 65/65; zero `Ai.Host` / `Microsoft.Extensions.AI` DLLs in `MissionEditor.Cli` output.

## 2. DRG-773 (`C2TopBarPanelHost` Projection Boundary) & DRG-717 (`LoopPolicyChromeBinder` Labels)
- **RED (pre-fix `C2TopBarPanelHost.cs` + `C2TopBarPanel.uxml`):**
  - `C2TopBarLoopPolicyHostContractTests.Top_bar_host_has_no_sim_scenario_import_or_direct_scenario_policy_read`:
    `DRG-773: C2TopBarPanelHost.cs must not import ProjectAegis.Sim.Scenario. Expected: not String containing "ProjectAegis.Sim.Scenario"`
  - `C2TopBarLoopPolicyHostContractTests.Top_bar_host_binds_loop_policy_chrome_via_projection`:
    `Expected: String containing "LoopPolicyChromeBinder.Project"`
  - `C2TopBarLoopPolicyHostContractTests.Top_bar_uxml_and_uss_declare_edit_policy_label_and_classes`:
    `Expected: String containing "name=\"edit-policy-label\""`
- **GREEN:** `UnitySimImportForbidTests` ratcheted 3→2 hosts (`C2TopBarPanelHost.cs` removed from Sim allowlist); `Sprint1UiWiringPlayModeTests.Top_bar_binds_fog_mode_and_loop_policy_chrome_via_projection` PASS in Unity Play Mode; live Play Mode verified `FOG: FULL PICTURE | EDITS: ANYTIME`.

## 3. DRG-370 (S124 Commit Strip/Bark Wiring, Coalesce Double `DROPPED`, `UNSPECIFIED` Log Parity)
- **RED (pre-fix `CommitRefusalBarkBinder.cs` + `RightUnitPanelHost.cs` + `UnitDetailPanel.uxml`):**
  - `CommitRefusalBarkBinderTests.Project_dropped_coalesces_correlated_policy_denial_and_synthetic_engagement_into_one_bark`:
    `Expected: property Count equal to 1 / But was: 2`
  - `CommitRefusalBarkBinderTests.Dropped_engagement_without_code_uses_unspecified_code`:
    `Expected: String containing "UNSPECIFIED" / But was: "Engagement aborted: "`
  - `CommitSurfaceHostContractTests.Right_unit_panel_wires_commit_strip_bark_and_runtime_tooltip_read_only`:
    `Expected: String containing "CommitConstraintStripBinder.BindRows"`
- **GREEN:** `CommitStripProjectionFenceTests` 7/7 + `Commit|C2TopBar|LoopPolicy|UnitDetail` 79/79 PASS; `Sprint1UiWiringPlayModeTests.Right_unit_panel_renders_commit_strip_and_dropped_bark_live` PASS; live Play Mode verified `COMMIT: READY | COST: 1 rd | MAG 4 -> 3`.

## 4. DRG-351 (Runtime UITK Tooltip Presenter in Play Mode)
- **RED (pre-fix `RightUnitPanelHost.cs` + Unity 6 PlayMode `FocusInEvent` catch):**
  - `CommitSurfaceHostContractTests.Unit_detail_layout_declares_commit_strip_bark_and_tooltip_styles` failed before adding `.unit-detail-runtime-tooltip`.
  - Unity PlayMode test `Runtime_tooltip_presenter_shows_and_hides_on_focus_and_hover_in_play_mode` initially failed with Unity 6 `FocusInEvent.PostDispatch` NRE, then passed cleanly using real `Focus()`/`Blur()` + `MouseEnterEvent`/`MouseLeaveEvent`.
- **GREEN:** `ProjectAegis.Unity.Tests` PlayMode **27/27 PASS**.

## 5. DRG-239 (Live Smoke-Scene Task Group & Authored Coverage Fixture)
- **RED (pre-fix `PlayModeSmokeOrbatSeeder.cs`):**
  - Initial compile RED (`CS0117`/`CS0103` on missing `TaskGroupId`, `PlayModeSmokeCoordinationFactsProvider`, `TrySeedWithEscortTaskGroup`).
  - Behavioral RED on detached member gap: `Assert.That(group.Coordination.GapCode, Is.EqualTo("SPLIT")) — Expected: "SPLIT", But was: "NONE"` (resolved by keeping escort wingman under task-group control rather than direct `HumanController`).
  - Architectural RED on `NamespaceSccBaselineTests.No_new_intra_assembly_namespace_cycles_vs_frozen_baseline` (resolved by placing provider in `CommandReview` namespace).
- **GREEN:** `PlayModeSmokeCoordinationFixtureTests` 7/7, `PlayModeSmokeHarnessTests` 27/27, `NamespaceSccBaselineTests` 2/2, `SmokeTaskGroupCoveragePlayModeTests` 2/2 PASS in Unity Play Mode.

## 6. DRG-781 (Unity Package Hygiene)
- Verified `manifest.json`, `manifest.template.json`, and `packages-lock.json` 0-drift lockstep (`UnityPackageManifestMirrorTests` 1/1 PASS; 0 console errors on Unity Editor package resolution).
