// Sprint 4 Track B PlayMode verification (DRG-346 UI wiring, DRG-274 G3 ME workflow, DRG-371 warnings).
// Exercises ScenarioEditorShellHost + BoundedAuthoringWorkflow + PlayEntrySession without rewriting ME core.
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using ProjectAegis.Unity.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Tests
{
    public sealed class Sprint4AuthoringPlayModeTests
    {
        private GameObject? _go;
        private PanelSettings? _settings;
        private ThemeStyleSheet? _theme;
        private string? _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Application.temporaryCachePath,
                $"aegis-s4-authoring-pm-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }

            if (_settings != null)
            {
                UnityEngine.Object.DestroyImmediate(_settings);
            }

            if (_theme != null)
            {
                UnityEngine.Object.DestroyImmediate(_theme);
            }

            _go = null;
            _settings = null;
            _theme = null;

            if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup of temp scenario copies.
                }
            }
        }

        private UIDocument CreateDocument(GameObject host)
        {
            var document = host.AddComponent<UIDocument>();
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            _settings.themeStyleSheet = _theme;
            document.panelSettings = _settings;
            return document;
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_unattached_keeps_d26_not_available_gating()
        {
            _go = new GameObject("sprint4-shell-unattached-test");
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            yield return null;

            Assert.IsFalse(host.IsAuthoringAttached);
            Assert.IsFalse(refs.BtnLoad.enabledSelf);
            StringAssert.StartsWith(ScenarioEditorActionAvailability.NotAvailablePrefix, refs.BtnLoad.tooltip);
            Assert.IsFalse(refs.BtnSave.enabledSelf);
            StringAssert.StartsWith(ScenarioEditorActionAvailability.NotAvailablePrefix, refs.BtnSave.tooltip);
            Assert.IsFalse(refs.BtnPlay.enabledSelf);
            StringAssert.StartsWith(ScenarioEditorActionAvailability.NotAvailablePrefix, refs.BtnPlay.tooltip);
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_supported_me_workflow_load_place_edit_save_undo_redo_reload_and_play_handoff()
        {
            var draftPath = CopyValidationFixture("bounded-authoring-supported.json");

            _go = new GameObject("sprint4-shell-supported-workflow-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: draftPath);
            yield return null;

            // Load via D26 Load button (NavigationSubmitEvent)
            Assert.IsTrue(refs.BtnLoad.enabledSelf, "Load button must be enabled when authoring workflow is attached.");
            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.IsTrue(host.HasOpenAuthoringSession);
            Assert.IsTrue(host.State.SessionOpen);
            Assert.AreEqual("Bounded Authoring Supported", refs.ScenarioTitle.text);
            Assert.AreEqual(2, host.AuthoringUnitCount);
            StringAssert.Contains("u1", refs.MapSlotSummary.text);

            // Select unit u1 and verify inspector reflects selection
            Assert.IsTrue(host.SelectAuthoringUnit("u1"));
            yield return null;
            StringAssert.Contains("u1", refs.InspectorBody.text);

            // Place new unit u2 and edit it -> reflected in map slot, top bar, and undo stack
            var placed = host.PlaceOrEditAuthoringUnit(new BoundedUnitEditRequest
            {
                UnitId = "u2",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.3,
                Lon = 20.4,
                AllowReplace = false,
            });
            Assert.IsTrue(placed.Succeeded, placed.Message);
            yield return null;

            Assert.AreEqual(3, host.AuthoringUnitCount);
            Assert.IsTrue(host.CanUndoAuthoring);
            Assert.IsTrue(refs.BtnUndo.enabledSelf);
            Assert.IsFalse(refs.BtnRedo.enabledSelf);
            StringAssert.Contains("u2", refs.MapSlotSummary.text);
            StringAssert.Contains("unsaved", refs.DirtyLabel.text);

            var edited = host.PlaceOrEditAuthoringUnit(new BoundedUnitEditRequest
            {
                UnitId = "u2",
                SideId = "blue",
                PlatformId = "ddg",
                Lat = 57.5,
                Lon = 20.6,
                AllowReplace = true,
            });
            Assert.IsTrue(edited.Succeeded, edited.Message);
            yield return null;

            // Save via D26 Save button
            Assert.IsTrue(refs.BtnSave.enabledSelf);
            SendSubmit(refs.BtnSave);
            yield return null;

            Assert.IsFalse(host.State.IsDirty);
            Assert.AreEqual("saved", refs.DirtyLabel.text);

            // Undo via D26 Undo button -> u2 reverts to ffg @ (57.3, 20.4)
            Assert.IsTrue(refs.BtnUndo.enabledSelf);
            SendSubmit(refs.BtnUndo);
            yield return null;

            Assert.IsTrue(host.TryGetAuthoringUnit("u2", out _, out var platformAfterUndo, out var latAfterUndo, out _));
            Assert.AreEqual("ffg", platformAfterUndo);
            Assert.AreEqual(57.3, latAfterUndo, 1e-4);
            Assert.IsTrue(host.CanRedoAuthoring);
            Assert.IsTrue(refs.BtnRedo.enabledSelf);

            // Redo via D26 Redo button -> u2 returns to ddg @ (57.5, 20.6)
            SendSubmit(refs.BtnRedo);
            yield return null;

            Assert.IsTrue(host.TryGetAuthoringUnit("u2", out _, out var platformAfterRedo, out var latAfterRedo, out _));
            Assert.AreEqual("ddg", platformAfterRedo);
            Assert.AreEqual(57.5, latAfterRedo, 1e-4);

            // Save again and verify persistence on fresh reload
            SendSubmit(refs.BtnSave);
            yield return null;
            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.IsTrue(host.TryGetAuthoringUnit("u2", out _, out var platformReloaded, out var latReloaded, out _));
            Assert.AreEqual("ddg", platformReloaded);
            Assert.AreEqual(57.5, latReloaded, 1e-4);

            // Trigger Play via D26 Play button -> export + PlayEntrySession handoff -> Executing + bridgeHost bound
            Assert.IsTrue(refs.BtnPlay.enabledSelf, $"Play should be enabled; gate={refs.PlayGate.text}");
            SendSubmit(refs.BtnPlay);
            yield return null;

            Assert.IsNotNull(host.LastPlayHandoff);
            Assert.IsTrue(host.LastPlayHandoff!.Succeeded, host.LastPlayHandoff.Message);
            Assert.IsNotNull(host.LastExportArtifactPath);
            Assert.IsTrue(File.Exists(host.LastExportArtifactPath!));
            Assert.IsNotNull(host.ActivePlayEntryState);
            Assert.IsTrue(host.ActivePlayEntryState!.HasPackage);
            Assert.AreEqual(SimulationPhase.Executing, host.ActivePlayEntryState.Phase);
            Assert.AreEqual(SimulationPhase.Executing, bridgeHost.Bridge.Phase);
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_validation_blocks_play_allows_save_surfaces_generic_warnings_and_recovers()
        {
            var invalidPath = CopyValidationFixture("bounded-authoring-invalid.json");
            var unsupportedPath = CopyValidationFixture("bounded-authoring-unsupported-role.json");
            var genericOnlyPath = CopyValidationFixture("role-capability-generic-only.json");

            _go = new GameObject("sprint4-shell-validation-recovery-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: invalidPath);
            yield return null;

            // 1. Invalid draft: Save remains enabled & writes draft; Play is blocked with explicit findings
            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.Greater(host.State.ErrorFindingCount, 0);
            Assert.IsTrue(host.State.PlayBlocked);
            Assert.IsTrue(refs.BtnSave.enabledSelf, "Save must stay enabled even on invalid drafts.");
            Assert.IsFalse(refs.BtnPlay.enabledSelf, "Play must be blocked on invalid drafts.");
            Assert.AreEqual(DisplayStyle.Flex, refs.PlayGate.style.display.value);
            Assert.Greater(refs.FindingsList.childCount, 0);

            // Modify invalid draft slightly or verify Save still succeeds on invalid draft
            SendSubmit(refs.BtnSave);
            yield return null;
            StringAssert.Contains("Draft saved", host.State.StatusSummary);

            // Recover invalid draft by assigning unit u1 to patrol-1
            var assigned = host.AssignAuthoringUnitToMission("patrol-1", "u1");
            Assert.IsTrue(assigned.Succeeded, assigned.Message);
            yield return null;

            Assert.AreEqual(0, host.State.ErrorFindingCount);
            Assert.IsFalse(host.State.PlayBlocked);
            Assert.IsTrue(refs.BtnPlay.enabledSelf, "Play must become enabled after recovery.");

            // 2. Unsupported role fixture: Save enabled, Play blocked with MISSION_ROLE_NOT_EXECUTED; recover by removing unsupported missions
            Assert.IsTrue(host.OpenAuthoringDraft(unsupportedPath));
            yield return null;

            Assert.IsTrue(host.HasUnsupportedExecution);
            Assert.Greater(host.State.ErrorFindingCount, 0);
            Assert.IsTrue(refs.BtnSave.enabledSelf);
            Assert.IsFalse(refs.BtnPlay.enabledSelf);

            SendSubmit(refs.BtnSave);
            yield return null;
            StringAssert.Contains("Draft saved", host.State.StatusSummary);

            Assert.IsTrue(host.RemoveAuthoringMission("support-tanker").Succeeded);
            Assert.IsTrue(host.RemoveAuthoringMission("support-aew").Succeeded);
            Assert.IsTrue(host.RemoveAuthoringMission("support-ew").Succeeded);
            yield return null;

            Assert.IsFalse(host.HasUnsupportedExecution);
            Assert.AreEqual(0, host.State.ErrorFindingCount);
            Assert.IsTrue(refs.BtnPlay.enabledSelf);

            SendSubmit(refs.BtnPlay);
            yield return null;
            Assert.IsTrue(host.LastPlayHandoff!.Succeeded, host.LastPlayHandoff.Message);
            Assert.AreEqual(SimulationPhase.Executing, host.ActivePlayEntryState!.Phase);

            // 3. GenericOnly warning tier (DRG-371) on cli-simulate-sample backend: WarningCount > 0, Play/Export remain enabled
            Assert.IsTrue(host.OpenAuthoringDraft(genericOnlyPath, backendId: "cli-simulate-sample"));
            yield return null;

            Assert.AreEqual(0, host.State.ErrorFindingCount);
            Assert.Greater(host.State.WarningFindingCount, 0);
            Assert.IsTrue(host.HasGenericExecutionWarnings);
            Assert.IsTrue(host.CanExportAuthoring);
            Assert.IsFalse(host.State.PlayBlocked);
            Assert.IsTrue(refs.BtnSave.enabledSelf);
            Assert.IsTrue(refs.BtnPlay.enabledSelf, "GenericOnly warnings must NOT block Play.");
            StringAssert.Contains("MISSION_ROLE_GENERIC_ONLY", JoinFindingsText(refs.FindingsList));

            SendSubmit(refs.BtnPlay);
            yield return null;
            Assert.IsTrue(host.LastPlayHandoff!.Succeeded, host.LastPlayHandoff.Message);
            Assert.AreEqual(SimulationPhase.Executing, host.ActivePlayEntryState!.Phase);
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_mixed_capability_tiers_reports_errors_and_warnings_without_claiming_play_allowed()
        {
            var mixedPath = CopyValidationFixture("role-capability-mixed-tiers.json");

            _go = new GameObject("sprint4-shell-mixed-tiers-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: mixedPath, defaultBackendId: "cli-simulate-sample");
            yield return null;

            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.IsTrue(host.HasOpenAuthoringSession);
            Assert.IsTrue(host.HasUnsupportedExecution);
            Assert.IsTrue(host.HasGenericExecutionWarnings);
            Assert.IsFalse(host.CanExportAuthoring);
            Assert.IsTrue(host.State.PlayBlocked);
            Assert.IsFalse(refs.BtnPlay.enabledSelf, "Play must be disabled when mixed unsupported role errors exist.");
            Assert.IsTrue(refs.BtnSave.enabledSelf, "Save must remain enabled on mixed-tier drafts.");

            StringAssert.Contains("unsupported role error(s)", refs.MapSlotSummary.text);
            StringAssert.Contains("generic-only warning(s)", refs.MapSlotSummary.text);
            StringAssert.DoesNotContain("Export and Play remain allowed", refs.MapSlotSummary.text);
            StringAssert.Contains("MISSION_ROLE_NOT_EXECUTED", JoinFindingsText(refs.FindingsList));
            StringAssert.Contains("MISSION_ROLE_GENERIC_ONLY", JoinFindingsText(refs.FindingsList));
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_failed_open_preserves_default_draft_path_and_updates_status()
        {
            var validPath = CopyValidationFixture("bounded-authoring-supported.json");
            var missingPath = Path.Combine(_tempDir!, "does-not-exist.json");

            _go = new GameObject("sprint4-shell-failed-open-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: validPath);
            yield return null;

            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.IsTrue(host.HasOpenAuthoringSession);
            Assert.AreEqual(2, host.AuthoringUnitCount);

            // Make an unsaved edit so reloading via BtnLoad observably reverts it to the valid draft.
            var placed = host.PlaceOrEditAuthoringUnit(new BoundedUnitEditRequest
            {
                UnitId = "u-unsaved",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.4,
                Lon = 20.5,
                AllowReplace = false,
            });
            Assert.IsTrue(placed.Succeeded, placed.Message);
            Assert.AreEqual(3, host.AuthoringUnitCount);

            var openedMissing = host.OpenAuthoringDraft(missingPath);
            yield return null;

            Assert.IsFalse(openedMissing);
            Assert.IsTrue(host.HasOpenAuthoringSession, "Prior valid session must remain open after failed open.");
            StringAssert.DoesNotContain("Opened", host.State.StatusSummary);
            StringAssert.Contains("not found", host.State.StatusSummary.ToLowerInvariant());
            StringAssert.Contains("not found", refs.Status.text.ToLowerInvariant());

            // Clicking BtnLoad must still reload validPath (reverting the unsaved unit), not missingPath.
            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.AreEqual(2, host.AuthoringUnitCount);
            StringAssert.StartsWith("Opened", host.State.StatusSummary);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_reenable_with_panel_asset_rewires_buttons_and_labels()
        {
            var draftPath = CopyValidationFixture("bounded-authoring-supported.json");
            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/ScenarioEditor/ScenarioEditorShell.uxml");
            Assert.IsNotNull(uxml, "Expected ScenarioEditorShell.uxml asset.");

            _go = new GameObject("sprint4-shell-reenable-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            document.visualTreeAsset = uxml;

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: draftPath);
            yield return null;

            _go.SetActive(false);
            yield return null;
            _go.SetActive(true);
            yield return null;

            var newRoot = document.rootVisualElement;
            Assert.IsNotNull(newRoot);
            var newBtnLoad = newRoot.Q<Button>("scenario-editor-shell-btn-load");
            var newTitle = newRoot.Q<Label>("scenario-editor-shell-scenario-title");
            var newMapSummary = newRoot.Q<Label>("scenario-editor-shell-map-summary");
            Assert.IsNotNull(newBtnLoad);
            Assert.IsNotNull(newTitle);
            Assert.IsNotNull(newMapSummary);
            Assert.IsTrue(newBtnLoad.enabledSelf, "Re-enabled Load button must be wired and enabled.");

            SendSubmit(newBtnLoad);
            yield return null;

            Assert.IsTrue(host.HasOpenAuthoringSession, "Clicking re-enabled Load button must open the draft.");
            Assert.AreEqual("Bounded Authoring Supported", newTitle.text);
            StringAssert.Contains("u1", newMapSummary.text);
        }
#endif

        [Test]
        public void ScenarioEditorShellHost_pre_awake_setters_do_not_throw()
        {
            _go = new GameObject("sprint4-shell-pre-awake-test");
            _go.SetActive(false);
            _go.AddComponent<UIDocument>();
            var host = _go.AddComponent<ScenarioEditorShellHost>();

            Assert.DoesNotThrow(() =>
            {
                host.SetMode(ScenarioEditorShellMode.MissionBoard);
                host.SetStatusSummary("Pre-awake status");
                host.SetSelectedEntityId("u1");
                host.SetFindings(1, 2);
                host.SetFindingsFilter(ScenarioEditorFindingsFilter.Errors);
            });

            Assert.AreEqual(ScenarioEditorShellMode.MissionBoard, host.State.Mode);
            Assert.AreEqual("Pre-awake status", host.State.StatusSummary);
            Assert.AreEqual("u1", host.State.SelectedEntityId);
        }

        [UnityTest]
        public IEnumerator ScenarioEditorShellHost_detach_reverts_all_six_d26_buttons_and_sample_exports_package()
        {
            var draftPath = CopyValidationFixture("bounded-authoring-supported.json");

            _go = new GameObject("sprint4-shell-detach-sample-test");
            var bridgeHost = _go.AddComponent<DelegationBridgeHost>();
            var document = CreateDocument(_go);
            BuildShellTree(document.rootVisualElement, out var refs);

            var host = _go.AddComponent<ScenarioEditorShellHost>();
            host.AttachDefaultAuthoringWorkflow(bridgeHost, defaultDraftPath: draftPath);
            yield return null;

            SendSubmit(refs.BtnLoad);
            yield return null;

            Assert.IsTrue(refs.BtnSample.enabledSelf, "Sample button must be enabled on valid draft.");
            SendSubmit(refs.BtnSample);
            yield return null;

            Assert.IsNotNull(host.LastExportArtifactPath, "BtnSample must export a package artifact.");
            Assert.IsTrue(File.Exists(host.LastExportArtifactPath!));

            host.DetachAuthoringWorkflow();
            yield return null;

            Assert.IsFalse(host.IsAuthoringAttached);
            Assert.AreEqual("Map Authoring surface slot", refs.MapSlotSummary.text);

            var buttons = new[] { refs.BtnLoad, refs.BtnSave, refs.BtnUndo, refs.BtnRedo, refs.BtnPlay, refs.BtnSample };
            foreach (var button in buttons)
            {
                Assert.IsFalse(button.enabledSelf, $"Button {button.name} must be disabled after DetachAuthoringWorkflow().");
                StringAssert.StartsWith(ScenarioEditorActionAvailability.NotAvailablePrefix, button.tooltip);
            }
        }

        private sealed class ShellRefs
        {
            public Label ScenarioTitle = null!;
            public Label DirtyLabel = null!;
            public Label EditVersion = null!;
            public Label ModeTitle = null!;
            public Label Status = null!;
            public Label PlayGate = null!;
            public Label InspectorBody = null!;
            public Label FindingsSummary = null!;
            public Label MapSlotSummary = null!;
            public ScrollView FindingsList = null!;
            public Button BtnLoad = null!;
            public Button BtnSave = null!;
            public Button BtnUndo = null!;
            public Button BtnRedo = null!;
            public Button BtnPlay = null!;
            public Button BtnSample = null!;
        }

        private static void BuildShellTree(VisualElement docRoot, out ShellRefs refs)
        {
            refs = new ShellRefs();
            var root = new VisualElement { name = "scenario-editor-shell-root" };
            var topbar = new VisualElement { name = "scenario-editor-shell-topbar" };

            refs.ScenarioTitle = new Label("Untitled Scenario") { name = "scenario-editor-shell-scenario-title" };
            refs.DirtyLabel = new Label("saved") { name = "scenario-editor-shell-dirty" };
            refs.EditVersion = new Label("v0") { name = "scenario-editor-shell-edit-version" };
            topbar.Add(refs.ScenarioTitle);
            topbar.Add(refs.DirtyLabel);
            topbar.Add(refs.EditVersion);

            var actions = new VisualElement { name = "scenario-editor-shell-topbar-actions" };
            refs.BtnLoad = new Button { name = "scenario-editor-shell-btn-load", text = "Load" };
            refs.BtnSave = new Button { name = "scenario-editor-shell-btn-save", text = "Save" };
            refs.BtnUndo = new Button { name = "scenario-editor-shell-btn-undo", text = "Undo" };
            refs.BtnRedo = new Button { name = "scenario-editor-shell-btn-redo", text = "Redo" };
            refs.BtnPlay = new Button { name = "scenario-editor-shell-btn-play", text = "Play" };
            refs.BtnSample = new Button { name = "scenario-editor-shell-btn-sample", text = "Sample" };
            actions.Add(refs.BtnLoad);
            actions.Add(refs.BtnSave);
            actions.Add(refs.BtnUndo);
            actions.Add(refs.BtnRedo);
            actions.Add(refs.BtnPlay);
            actions.Add(refs.BtnSample);
            topbar.Add(actions);
            root.Add(topbar);

            var header = new VisualElement { name = "scenario-editor-shell-header" };
            refs.ModeTitle = new Label("MAP AUTHORING") { name = "scenario-editor-shell-mode-title" };
            refs.Status = new Label(string.Empty) { name = "scenario-editor-shell-status" };
            header.Add(refs.ModeTitle);
            header.Add(refs.Status);
            root.Add(header);

            var tabs = new VisualElement { name = "scenario-editor-shell-tabs", focusable = true };
            tabs.Add(new Button { name = "scenario-editor-shell-tab-map", text = "Map" });
            tabs.Add(new Button { name = "scenario-editor-shell-tab-missions", text = "Missions" });
            tabs.Add(new Button { name = "scenario-editor-shell-tab-events", text = "Events" });
            refs.PlayGate = new Label(string.Empty) { name = "scenario-editor-shell-play-gate" };
            tabs.Add(refs.PlayGate);
            root.Add(tabs);

            var body = new VisualElement { name = "scenario-editor-shell-body" };
            var drawer = new VisualElement { name = "scenario-editor-shell-drawer" };
            drawer.Add(new Label("ORBAT / Zones") { name = "scenario-editor-shell-drawer-title" });
            body.Add(drawer);

            var content = new VisualElement { name = "scenario-editor-shell-content" };
            var mapSlot = new VisualElement { name = "scenario-editor-shell-map-slot" };
            refs.MapSlotSummary = new Label("Map Authoring surface slot") { name = "scenario-editor-shell-map-summary" };
            mapSlot.Add(refs.MapSlotSummary);
            content.Add(mapSlot);
            var boardSlot = new VisualElement { name = "scenario-editor-shell-mission-board-slot" };
            content.Add(boardSlot);
            body.Add(content);

            var inspector = new VisualElement { name = "scenario-editor-shell-inspector" };
            refs.InspectorBody = new Label("No selection") { name = "scenario-editor-shell-inspector-body" };
            inspector.Add(refs.InspectorBody);
            body.Add(inspector);
            root.Add(body);

            var dock = new VisualElement { name = "scenario-editor-shell-findings-dock" };
            refs.FindingsSummary = new Label("Findings: 0 errors · 0 warnings") { name = "scenario-editor-shell-findings-summary" };
            dock.Add(refs.FindingsSummary);
            dock.Add(new Button { name = "scenario-editor-shell-filter-all", text = "All" });
            dock.Add(new Button { name = "scenario-editor-shell-filter-errors", text = "Errors" });
            dock.Add(new Button { name = "scenario-editor-shell-filter-warnings", text = "Warnings" });
            refs.FindingsList = new ScrollView { name = "scenario-editor-shell-findings-list" };
            dock.Add(refs.FindingsList);
            root.Add(dock);

            docRoot.Add(root);
        }

        private string CopyValidationFixture(string fileName)
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            var src = Path.Combine(repoRoot, "assets", "data", "scenarios", "validation", fileName);
            Assert.IsTrue(File.Exists(src), $"Missing validation fixture: {src}");
            var dest = Path.Combine(_tempDir!, $"{Guid.NewGuid():N}-{fileName}");
            File.Copy(src, dest, overwrite: true);
            return dest;
        }

        private static string JoinFindingsText(ScrollView findingsList)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var row in findingsList.Children())
            {
                foreach (var child in row.Children())
                {
                    if (child is Label label)
                    {
                        sb.Append(label.text).Append(' ');
                    }
                }
            }

            return sb.ToString();
        }

        private static void SendSubmit(Button button)
        {
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }
    }
}
#endif
