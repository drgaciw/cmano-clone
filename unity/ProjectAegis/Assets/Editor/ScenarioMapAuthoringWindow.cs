#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Editor
{
    /// <summary>
    /// Thin Edit Mode host for Phase 2.1 scenario map authoring (Task 9 / ME-W0).
    /// UI Toolkit chrome from <c>Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uxml</c>
    /// (UI Builder editable). Binds only to headless APIs:
    /// <see cref="ScenarioAuthoringSession"/>, <see cref="ScenarioEditCommandBus"/>,
    /// <see cref="MapAuthoringSurface"/>, <see cref="LiveFindingsPresenter"/>.
    /// No Validation Engine rules, no <c>DelegationBridge</c>, no business logic.
    /// </summary>
    public sealed class ScenarioMapAuthoringWindow : EditorWindow
    {
        /// <summary>Project-relative path for the UI Builder UXML asset.</summary>
        public const string PanelUxmlAssetPath =
            "Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uxml";

        /// <summary>Project-relative path for the companion USS stylesheet.</summary>
        public const string PanelUssAssetPath =
            "Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.uss";

        private string _scenarioPath = string.Empty;
        private string _status = "Browse… a scenario.json then Open.";

        private ScenarioAuthoringSession? _session;
        private MapAuthoringSurface? _surface;
        private LiveFindingsPresenter? _findings;

        private TextField? _pathField;
        private Label? _sessionPathLabel;
        private Label? _sessionEditVersionLabel;
        private Label? _sessionDirtyLabel;
        private Label? _statusLabel;
        private Label? _exportGateLabel;
        private VisualElement? _sessionBody;
        private VisualElement? _unitsList;
        private VisualElement? _rpsList;
        private VisualElement? _findingsList;
        private TextField? _placeUnitId;
        private TextField? _placeSideId;
        private TextField? _placePlatformId;
        private TextField? _placeLat;
        private TextField? _placeLon;
        private VisualElement? _platformList;
        private Label? _platformDomainLabel;
        private Button? _domainAircraftButton;
        private Button? _domainShipsButton;
        private Button? _domainSubsButton;
        private Button? _domainGroundButton;
        private TextField? _rpId;
        private TextField? _rpLat0;
        private TextField? _rpLon0;
        private TextField? _rpLat1;
        private TextField? _rpLon1;
        private TextField? _rpLat2;
        private TextField? _rpLon2;
        private Button? _saveButton;
        private Button? _rebuildButton;
        private Button? _refreshFindingsButton;

        private string _activeDomainId = ScenarioPlatformDomainCatalog.DomainShips;
        private string? _selectedPlatformId;
        private int _unitIdSequence = 1;

        /// <summary>
        /// Local chrome policy mirrors (keep Editor compiling even if plugin DLL is stale
        /// until <c>./tools/copy-delegation-assemblies.sh</c> is run). Headless tests cover
        /// <see cref="ScenarioMapAuthoringHostPolicy"/> as the source of truth.
        /// </summary>
        private const string NoSessionMessage = "Open a scenario.json first (Browse… then Open).";

        // Export gate modifier classes (see ScenarioMapAuthoringPanel.uss).
        private const string ExportGateBlockedClass = "scenario-map-export-gate--blocked";
        private const string ExportGateReadyClass = "scenario-map-export-gate--ready";

        private static readonly string[] DefaultScenarioRelativeCandidates =
        {
            "assets/data/scenarios/authoring/russia-vs-nato-baltic-ui-dev.json",
            "assets/data/scenarios/validation/golden_clean.json",
        };

        private static bool ShouldEnableCatalogAndFormChrome(bool hasSession) => true;

        private static bool ShouldEnableSessionWriteActions(bool hasSession) => hasSession;

        private static string? ResolveDefaultScenarioPath(string? repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot) || !Directory.Exists(repoRoot))
            {
                return null;
            }

            foreach (var rel in DefaultScenarioRelativeCandidates)
            {
                var full = Path.GetFullPath(Path.Combine(repoRoot, rel));
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return null;
        }

        /// <summary>Opens the Scenario Map Authoring window from the menu.</summary>
        [MenuItem("Project Aegis/Scenario Map Authoring")]
        public static void OpenWindow() => OpenWindowForDomain(ScenarioPlatformDomainCatalog.DomainShips);

        [MenuItem("Project Aegis/Scenario Editor/Add Aircraft…")]
        public static void OpenAddAircraft() => OpenWindowForDomain(ScenarioPlatformDomainCatalog.DomainAircraft);

        [MenuItem("Project Aegis/Scenario Editor/Add Ships…")]
        public static void OpenAddShips() => OpenWindowForDomain(ScenarioPlatformDomainCatalog.DomainShips);

        [MenuItem("Project Aegis/Scenario Editor/Add Subs…")]
        public static void OpenAddSubs() => OpenWindowForDomain(ScenarioPlatformDomainCatalog.DomainSubs);

        [MenuItem("Project Aegis/Scenario Editor/Add Ground Units…")]
        public static void OpenAddGround() => OpenWindowForDomain(ScenarioPlatformDomainCatalog.DomainGround);

        /// <summary>Opens the authoring host focused on a platform domain catalog.</summary>
        public static void OpenWindowForDomain(string domainId)
        {
            var window = GetWindow<ScenarioMapAuthoringWindow>();
            window.titleContent = new GUIContent("Scenario Map Authoring");
            window.minSize = new Vector2(480f, 760f);
            // Ensure enough room for stacked UI Toolkit chrome after IMGUI → UITK migration.
            if (window.position.height < 700f || window.position.width < 480f)
            {
                window.position = new Rect(window.position.x, window.position.y, 520f, 800f);
            }

            window._activeDomainId = string.IsNullOrWhiteSpace(domainId)
                ? ScenarioPlatformDomainCatalog.DomainShips
                : domainId.Trim().ToLowerInvariant();
            window.Show();
            // Rebuild every open so UXML/USS edits (UI Builder) apply without domain reload.
            window.RebuildUi();
            // Seed + open UI-dev Baltic scenario so Save/Place/RP buttons are live immediately.
            window.TryAutoOpenSeededScenario();
            window.Focus();
            window.Repaint();
        }

        /// <summary>Menu: open window and force-load the Baltic UI-dev scenario.</summary>
        [MenuItem("Project Aegis/Scenario Editor/Open Baltic UI Dev Scenario")]
        public static void OpenBalticUiDevScenario()
        {
            var window = GetWindow<ScenarioMapAuthoringWindow>();
            window.titleContent = new GUIContent("Scenario Map Authoring");
            window.minSize = new Vector2(480f, 760f);
            window._activeDomainId = ScenarioPlatformDomainCatalog.DomainShips;
            window.Show();
            window.RebuildUi();
            window.TryLoadUiDevScenario(force: true);
            window.Focus();
            window.Repaint();
        }

        /// <summary>
        /// Headless/agent open: write <c>Temp/aegis-open-scenario-map-authoring.flag</c>
        /// then refresh assets; flag is consumed once on the next editor update.
        /// Optional flag body:
        /// <list type="bullet">
        /// <item><c>ui-dev</c> / <c>baltic</c> / empty → open + auto-load Baltic UI-dev scenario</item>
        /// <item>absolute path to a <c>.json</c> scenario → open that file</item>
        /// </list>
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OpenWindowIfAgentFlagPresent()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var flagPath = Path.Combine(Application.dataPath, "..", "Temp",
                        "aegis-open-scenario-map-authoring.flag");
                    flagPath = Path.GetFullPath(flagPath);
                    if (!File.Exists(flagPath))
                    {
                        return;
                    }

                    var body = (File.ReadAllText(flagPath) ?? string.Empty).Trim();
                    File.Delete(flagPath);

                    // Prefer Baltic UI-dev load so Save/Place/RP chrome is live for agent UAT.
                    if (string.IsNullOrEmpty(body)
                        || body.Equals("ui-dev", StringComparison.OrdinalIgnoreCase)
                        || body.Equals("baltic", StringComparison.OrdinalIgnoreCase)
                        || body.Equals("load-ui-dev", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenBalticUiDevScenario();
                        Debug.Log("ScenarioMapAuthoringWindow: opened Baltic UI-dev scenario via agent flag.");
                        return;
                    }

                    if (File.Exists(body) && body.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        var window = GetWindow<ScenarioMapAuthoringWindow>();
                        window.titleContent = new GUIContent("Scenario Map Authoring");
                        window.minSize = new Vector2(480f, 760f);
                        window._activeDomainId = ScenarioPlatformDomainCatalog.DomainShips;
                        window.Show();
                        window.RebuildUi();
                        window._scenarioPath = Path.GetFullPath(body);
                        window.ApplyPathToField();
                        window.TryOpen();
                        window.Focus();
                        window.Repaint();
                        Debug.Log($"ScenarioMapAuthoringWindow: opened scenario via agent flag path: {body}");
                        return;
                    }

                    OpenWindow();
                    Debug.Log($"ScenarioMapAuthoringWindow: opened via agent flag (unrecognized body '{body}').");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"ScenarioMapAuthoringWindow agent open failed: {ex.Message}");
                }
            };
        }

        private void OnDisable()
        {
            DisposeSession();
        }

        private void CreateGUI()
        {
            RebuildUi();
        }

        /// <summary>Clone shipped UXML/USS and rebind named controls (UI Builder–compatible assets).</summary>
        private void RebuildUi()
        {
            // UXML reclones form defaults; drop staged gestures so Commit cannot upsert stale TentativeUnit.
            ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForChromeRebuild(_surface);

            rootVisualElement.Clear();

            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PanelUxmlAssetPath);
            if (tree == null)
            {
                rootVisualElement.Add(new Label(
                    $"Missing UXML at {PanelUxmlAssetPath}. Refresh Assets or open UI Builder."));
                return;
            }

            tree.CloneTree(rootVisualElement);

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(PanelUssAssetPath);
            if (style != null && !rootVisualElement.styleSheets.Contains(style))
            {
                rootVisualElement.styleSheets.Add(style);
            }

            CacheControls(rootVisualElement);
            WireCallbacks();
            TrySeedDefaultScenarioPath();
            ApplyPathToField();
            RefreshSessionChrome();
            RefreshLists();
            SelectDomain(_activeDomainId, preserveSelection: true);
            SetStatus(_status);
        }

        private void CacheControls(VisualElement root)
        {
            _pathField = root.Q<TextField>("scenario-path-field");
            _sessionPathLabel = root.Q<Label>("scenario-session-path");
            _sessionEditVersionLabel = root.Q<Label>("scenario-session-edit-version");
            _sessionDirtyLabel = root.Q<Label>("scenario-session-dirty");
            _statusLabel = root.Q<Label>("scenario-status");
            _exportGateLabel = root.Q<Label>("scenario-export-gate");
            _sessionBody = root.Q<VisualElement>("scenario-session-body");
            _unitsList = root.Q<VisualElement>("scenario-units-list");
            _rpsList = root.Q<VisualElement>("scenario-rps-list");
            _findingsList = root.Q<VisualElement>("scenario-findings-list");
            _placeUnitId = root.Q<TextField>("scenario-place-unit-id");
            _placeSideId = root.Q<TextField>("scenario-place-side-id");
            _placePlatformId = root.Q<TextField>("scenario-place-platform-id");
            _placeLat = root.Q<TextField>("scenario-place-lat");
            _placeLon = root.Q<TextField>("scenario-place-lon");
            _platformList = root.Q<VisualElement>("scenario-platform-list");
            _platformDomainLabel = root.Q<Label>("scenario-platform-domain-label");
            _domainAircraftButton = root.Q<Button>("scenario-domain-aircraft");
            _domainShipsButton = root.Q<Button>("scenario-domain-ships");
            _domainSubsButton = root.Q<Button>("scenario-domain-subs");
            _domainGroundButton = root.Q<Button>("scenario-domain-ground");
            _rpId = root.Q<TextField>("scenario-rp-id");
            _rpLat0 = root.Q<TextField>("scenario-rp-lat0");
            _rpLon0 = root.Q<TextField>("scenario-rp-lon0");
            _rpLat1 = root.Q<TextField>("scenario-rp-lat1");
            _rpLon1 = root.Q<TextField>("scenario-rp-lon1");
            _rpLat2 = root.Q<TextField>("scenario-rp-lat2");
            _rpLon2 = root.Q<TextField>("scenario-rp-lon2");
            _saveButton = root.Q<Button>("scenario-save-button");
            _rebuildButton = root.Q<Button>("scenario-rebuild-button");
            _refreshFindingsButton = root.Q<Button>("scenario-refresh-findings-button");
        }

        private void WireCallbacks()
        {
            // Dual-wire: Button.clicked (PlatformImportPanelHost pattern) + ClickEvent so
            // Editor UITK never drops presses when one path is blocked by focus/scroll.
            WireButton(rootVisualElement.Q<Button>("scenario-browse-button"), BrowsePath);
            WireButton(rootVisualElement.Q<Button>("scenario-open-button"), () =>
            {
                SyncPathFromField();
                TryOpen();
            });
            WireButton(rootVisualElement.Q<Button>("scenario-load-ui-dev-button"), () =>
                TryLoadUiDevScenario(force: true));
            WireButton(_saveButton, TrySave);
            WireButton(_rebuildButton, TryRebuild);
            WireButton(_refreshFindingsButton, TryRefreshFindings);
            WireButton(rootVisualElement.Q<Button>("scenario-begin-place-unit"), TryBeginPlaceUnit);
            WireButton(rootVisualElement.Q<Button>("scenario-commit-place-unit"), TryCommitPlaceUnit);
            WireButton(rootVisualElement.Q<Button>("scenario-place-and-commit"), TryPlaceAndCommit);
            WireButton(rootVisualElement.Q<Button>("scenario-cancel-gesture"), TryCancelGesture);
            WireButton(rootVisualElement.Q<Button>("scenario-upsert-rp"), TryUpsertRpPolygon);

            WireButton(_domainAircraftButton, () =>
                SelectDomain(ScenarioPlatformDomainCatalog.DomainAircraft));
            WireButton(_domainShipsButton, () =>
                SelectDomain(ScenarioPlatformDomainCatalog.DomainShips));
            WireButton(_domainSubsButton, () =>
                SelectDomain(ScenarioPlatformDomainCatalog.DomainSubs));
            WireButton(_domainGroundButton, () =>
                SelectDomain(ScenarioPlatformDomainCatalog.DomainGround));

            _pathField?.RegisterValueChangedCallback(evt => _scenarioPath = evt.newValue ?? string.Empty);
        }

        private static void WireButton(Button? button, Action handler)
        {
            if (button == null)
            {
                return;
            }

            // Always pickable/enabled for primary chrome (session write gates re-applied in RefreshSessionChrome).
            button.pickingMode = PickingMode.Position;
            button.focusable = true;

            // Guard against double-fire when both clicked and ClickEvent raise for one press.
            var lastTicks = 0L;
            void Invoke()
            {
                var now = DateTime.UtcNow.Ticks;
                if (now - lastTicks < TimeSpan.TicksPerMillisecond * 120)
                {
                    return;
                }

                lastTicks = now;
                handler();
            }

            button.clicked += Invoke;
            button.RegisterCallback<ClickEvent>(evt =>
            {
                Invoke();
                evt.StopPropagation();
            });
        }

        /// <summary>
        /// When the path field is empty, seed the Baltic UI-dev scenario (then golden_clean).
        /// </summary>
        private void TrySeedDefaultScenarioPath()
        {
            if (!string.IsNullOrWhiteSpace(_scenarioPath))
            {
                return;
            }

            var repoRoot = ResolveRepoRootFromDataPath();
            var candidate = ResolveDefaultScenarioPath(repoRoot);
            if (!string.IsNullOrEmpty(candidate))
            {
                _scenarioPath = candidate;
            }
        }

        /// <summary>Application.dataPath → …/ProjectAegis/Assets; repo root is three levels up.</summary>
        private static string? ResolveRepoRootFromDataPath()
        {
            try
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// After chrome rebuild: seed path and open session so domain/place/save are live.
        /// No-ops when a session is already open (avoids clobbering dirty edits).
        /// </summary>
        private void TryAutoOpenSeededScenario()
        {
            if (_session != null)
            {
                return;
            }

            TrySeedDefaultScenarioPath();
            ApplyPathToField();
            if (string.IsNullOrWhiteSpace(_scenarioPath) || !File.Exists(_scenarioPath))
            {
                SetStatus("Browse… a scenario.json then Open. (No default UI-dev path found.)");
                return;
            }

            TryOpen();
        }

        /// <summary>Force path to UI-dev Baltic scenario and open (menu / Load UI Dev button).</summary>
        private void TryLoadUiDevScenario(bool force)
        {
            var repoRoot = ResolveRepoRootFromDataPath();
            var uiDev = ResolveDefaultScenarioPath(repoRoot);
            if (string.IsNullOrEmpty(uiDev) || !uiDev.Contains("russia-vs-nato-baltic-ui-dev", StringComparison.OrdinalIgnoreCase))
            {
                // Prefer explicit UI-dev even if golden was first-hit after delete; re-resolve candidates.
                if (!string.IsNullOrEmpty(repoRoot))
                {
                    var explicitPath = Path.GetFullPath(Path.Combine(
                        repoRoot,
                        "assets", "data", "scenarios", "authoring",
                        "russia-vs-nato-baltic-ui-dev.json"));
                    if (File.Exists(explicitPath))
                    {
                        uiDev = explicitPath;
                    }
                }
            }

            if (string.IsNullOrEmpty(uiDev) || !File.Exists(uiDev))
            {
                SetStatus("UI-dev scenario not found: assets/data/scenarios/authoring/russia-vs-nato-baltic-ui-dev.json");
                return;
            }

            if (!force && _session != null && string.Equals(_session.Path, uiDev, StringComparison.OrdinalIgnoreCase))
            {
                SetStatus($"Already open: {_session.Path}");
                RefreshAllUi();
                return;
            }

            _scenarioPath = uiDev;
            ApplyPathToField();
            TryOpen();
        }

        private void BrowsePath()
        {
            SyncPathFromField();
            var picked = EditorUtility.OpenFilePanel(
                "Open scenario.json",
                string.IsNullOrEmpty(_scenarioPath)
                    ? Application.dataPath
                    : Path.GetDirectoryName(_scenarioPath) ?? Application.dataPath,
                "json");
            if (!string.IsNullOrEmpty(picked))
            {
                _scenarioPath = picked;
                ApplyPathToField();
            }
        }

        private void SyncPathFromField()
        {
            if (_pathField != null)
            {
                _scenarioPath = _pathField.value ?? string.Empty;
            }
        }

        private void ApplyPathToField()
        {
            if (_pathField != null)
            {
                _pathField.SetValueWithoutNotify(_scenarioPath ?? string.Empty);
            }
        }

        private void SetStatus(string message)
        {
            _status = message ?? string.Empty;
            if (_statusLabel != null)
            {
                _statusLabel.text = _status;
            }
        }

        private void RefreshSessionChrome()
        {
            var hasSession = _session != null;
            // Keep catalog/form/domain chrome enabled without a session so place/RP buttons
            // still respond and can surface NoSessionMessage (instead of looking dead).
            // Belt-and-suspenders: never leave the body disabled even if policy changes.
            if (_sessionBody != null)
            {
                _sessionBody.SetEnabled(ShouldEnableCatalogAndFormChrome(hasSession));
                _sessionBody.pickingMode = PickingMode.Position;
            }

            var writeEnabled = ShouldEnableSessionWriteActions(hasSession);
            _saveButton?.SetEnabled(writeEnabled);
            _rebuildButton?.SetEnabled(writeEnabled);
            _refreshFindingsButton?.SetEnabled(writeEnabled);
            // Open / Browse / Load UI Dev always remain enabled.
            rootVisualElement.Q<Button>("scenario-open-button")?.SetEnabled(true);
            rootVisualElement.Q<Button>("scenario-browse-button")?.SetEnabled(true);
            rootVisualElement.Q<Button>("scenario-load-ui-dev-button")?.SetEnabled(true);

            if (_session == null)
            {
                if (_sessionPathLabel != null)
                {
                    _sessionPathLabel.text = "Path: —";
                }

                if (_sessionEditVersionLabel != null)
                {
                    _sessionEditVersionLabel.text = "Edit version: —";
                }

                if (_sessionDirtyLabel != null)
                {
                    _sessionDirtyLabel.text = "Dirty: —";
                }

                return;
            }

            if (_sessionPathLabel != null)
            {
                _sessionPathLabel.text = $"Path: {_session.Path}";
            }

            if (_sessionEditVersionLabel != null)
            {
                _sessionEditVersionLabel.text = $"Edit version: {_session.EditVersion}";
            }

            if (_sessionDirtyLabel != null)
            {
                _sessionDirtyLabel.text = $"Dirty: {(_session.IsDirty ? "yes" : "no")}";
            }
        }

        private void RefreshLists()
        {
            FillScroll(_unitsList, BuildUnitLines());
            FillScroll(_rpsList, BuildRpLines());
            FillScroll(_findingsList, BuildFindingsLines());
            RefreshExportGateChrome();
        }

        /// <summary>
        /// Renders the validation export gate (ADR-008 / DRG-56).
        ///
        /// The decision and its wording are owned by <see cref="LiveFindingsPresenter"/>; this
        /// method only paints them. It must never re-derive the gate from raw severities, and it
        /// must never disable Save — save is deliberately ungated so work-in-progress with
        /// blocking findings can still be persisted (AME-6.5).
        /// </summary>
        private void RefreshExportGateChrome()
        {
            if (_exportGateLabel == null)
            {
                return;
            }

            _exportGateLabel.RemoveFromClassList(ExportGateBlockedClass);
            _exportGateLabel.RemoveFromClassList(ExportGateReadyClass);

            if (_findings?.LastReport == null)
            {
                _exportGateLabel.text = "Export gate: not validated yet.";
                _exportGateLabel.tooltip = "Open a scenario and Refresh Findings to evaluate the export gate.";
                return;
            }

            var gate = _findings.Gate;
            var counts = DescribeGateCounts(gate);

            if (gate.CanExport)
            {
                _exportGateLabel.text = $"READY TO EXPORT — {counts}";
                _exportGateLabel.tooltip =
                    "No blocking findings. Export, publish, brief and simulate-sample are permitted.";
                _exportGateLabel.AddToClassList(ExportGateReadyClass);
                return;
            }

            _exportGateLabel.text = $"EXPORT BLOCKED — {counts}";
            _exportGateLabel.tooltip = gate.BlockingReason;
            _exportGateLabel.AddToClassList(ExportGateBlockedClass);
        }

        private static string DescribeGateCounts(ScenarioExportGateState gate)
        {
            var errors = gate.ErrorCount == 1 ? "1 error" : $"{gate.ErrorCount} errors";
            var warnings = gate.WarningCount == 1 ? "1 warning" : $"{gate.WarningCount} warnings";
            return $"{errors}, {warnings}";
        }

        private IReadOnlyList<string> BuildUnitLines()
        {
            if (_surface == null || _surface.Units.Count == 0)
            {
                return Array.Empty<string>();
            }

            var lines = new List<string>(_surface.Units.Count);
            for (var i = 0; i < _surface.Units.Count; i++)
            {
                var u = _surface.Units[i];
                lines.Add($"{u.UnitId}  side={u.SideId}  platform={u.PlatformId}  ({u.Lat:F4},{u.Lon:F4})");
            }

            return lines;
        }

        private IReadOnlyList<string> BuildRpLines()
        {
            if (_surface == null || _surface.ReferencePoints.Count == 0)
            {
                return Array.Empty<string>();
            }

            var lines = new List<string>(_surface.ReferencePoints.Count);
            for (var i = 0; i < _surface.ReferencePoints.Count; i++)
            {
                var rp = _surface.ReferencePoints[i];
                var valid = rp.IsGeometryValid ? "valid" : $"invalid:{rp.InvalidReason}";
                lines.Add($"{rp.Id}  type={rp.Type}  {valid}");
            }

            return lines;
        }

        private IReadOnlyList<string> BuildFindingsLines()
        {
            if (_findings == null || _findings.LastCodes.Count == 0)
            {
                return Array.Empty<string>();
            }

            var lines = new List<string>(_findings.LastCodes.Count);
            for (var i = 0; i < _findings.LastCodes.Count; i++)
            {
                lines.Add(_findings.LastCodes[i]);
            }

            return lines;
        }

        private static void FillScroll(VisualElement? list, IReadOnlyList<string> lines)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            if (lines.Count == 0)
            {
                var empty = new Label("(none)") { name = "list-empty", pickingMode = PickingMode.Ignore };
                empty.AddToClassList("scenario-map-list-empty");
                list.Add(empty);
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var label = new Label(lines[i]) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("scenario-map-list-item");
                list.Add(label);
            }
        }

        private void RefreshAllUi()
        {
            RefreshSessionChrome();
            RefreshLists();
            SetStatus(_status);
            rootVisualElement.MarkDirtyRepaint();
        }

        private string FieldValue(TextField? field, string fallback = "")
        {
            return field?.value ?? fallback;
        }

        private void TryOpen()
        {
            try
            {
                SyncPathFromField();
                if (string.IsNullOrWhiteSpace(_scenarioPath))
                {
                    // Empty path → open file dialog once; if still empty, guide the user.
                    BrowsePath();
                    SyncPathFromField();
                    if (string.IsNullOrWhiteSpace(_scenarioPath))
                    {
                        SetStatus("Browse… a scenario.json then Open.");
                        return;
                    }
                }

                if (!File.Exists(_scenarioPath))
                {
                    SetStatus($"File not found: {_scenarioPath}");
                    return;
                }

                DisposeSession();
                _session = ScenarioAuthoringSession.Open(_scenarioPath);
                _surface = new MapAuthoringSurface(_session);
                _findings = new LiveFindingsPresenter(_session, debounceMs: 0);
                _surface.RebuildFromDocument();
                _findings.RefreshImmediate();
                SetStatus($"Opened {_session.Path} (editVersion={_session.EditVersion}).");
                RefreshAllUi();
            }
            catch (Exception ex)
            {
                DisposeSession();
                SetStatus($"Open failed: {ex.Message}");
                RefreshAllUi();
            }
        }

        private void TrySave()
        {
            if (_session == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            try
            {
                _session.Save();
                SetStatus($"Saved {_session.Path} (editVersion={_session.EditVersion}).");
                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Save failed: {ex.Message}");
            }
        }

        private void TryRebuild()
        {
            if (_surface == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            try
            {
                _surface.RebuildFromDocument();
                SetStatus($"Rebuilt surface: {_surface.Units.Count} units, {_surface.ReferencePoints.Count} RPs.");
                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Rebuild failed: {ex.Message}");
            }
        }

        private void TryRefreshFindings()
        {
            if (_findings == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            try
            {
                _findings.RefreshImmediate();
                SetStatus($"Findings: {_findings.LastCodes.Count} code(s).");
                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Refresh findings failed: {ex.Message}");
            }
        }

        private void SelectDomain(string domainId, bool preserveSelection = false)
        {
            var nextDomain = string.IsNullOrWhiteSpace(domainId)
                ? ScenarioPlatformDomainCatalog.DomainShips
                : domainId.Trim().ToLowerInvariant();
            // Domain switch rewrites the place form / catalog; drop staged unit so Commit cannot
            // upsert the pre-switch TentativeUnit (UAT stale-gesture class).
            if (!string.Equals(_activeDomainId, nextDomain, StringComparison.Ordinal)
                && !preserveSelection)
            {
                ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForFormOrDomainChange(_surface);
            }

            _activeDomainId = nextDomain;

            SetDomainButtonActive(_domainAircraftButton, _activeDomainId == ScenarioPlatformDomainCatalog.DomainAircraft);
            SetDomainButtonActive(_domainShipsButton, _activeDomainId == ScenarioPlatformDomainCatalog.DomainShips);
            SetDomainButtonActive(_domainSubsButton, _activeDomainId == ScenarioPlatformDomainCatalog.DomainSubs);
            SetDomainButtonActive(_domainGroundButton, _activeDomainId == ScenarioPlatformDomainCatalog.DomainGround);

            if (_platformDomainLabel != null)
            {
                _platformDomainLabel.text =
                    $"Domain: {ScenarioPlatformDomainCatalog.DomainLabel(_activeDomainId)}";
            }

            FillPlatformCatalog(preserveSelection);
        }

        private static void SetDomainButtonActive(Button? button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList("scenario-map-domain-button--active", active);
        }

        private void FillPlatformCatalog(bool preserveSelection)
        {
            if (_platformList == null)
            {
                return;
            }

            _platformList.Clear();
            var presets = ScenarioPlatformDomainCatalog.GetPresets(_activeDomainId);
            if (presets.Count == 0)
            {
                var empty = new Label("(no platforms in domain)")
                {
                    name = "platform-list-empty",
                    pickingMode = PickingMode.Ignore,
                };
                empty.AddToClassList("scenario-map-list-empty");
                _platformList.Add(empty);
                return;
            }

            var stillSelected = false;
            for (var i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                var row = new Button { text = preset.ListLine, userData = preset };
                row.AddToClassList("scenario-map-platform-row");
                if (preserveSelection
                    && !string.IsNullOrEmpty(_selectedPlatformId)
                    && string.Equals(_selectedPlatformId, preset.PlatformId, StringComparison.OrdinalIgnoreCase))
                {
                    row.AddToClassList("scenario-map-platform-row--selected");
                    stillSelected = true;
                }

                var captured = preset;
                row.clicked += () => ApplyPlatformPreset(captured);
                _platformList.Add(row);
            }

            if (!preserveSelection || !stillSelected)
            {
                // Auto-select first preset when domain changes so place form is never blank.
                ApplyPlatformPreset(presets[0]);
            }
        }

        private void ApplyPlatformPreset(ScenarioPlatformPreset preset)
        {
            // Catalog click rewrites platform/side/id fields — invalidate any staged place gesture.
            if (_surface?.TentativeUnit != null
                && !string.Equals(_selectedPlatformId, preset.PlatformId, StringComparison.OrdinalIgnoreCase))
            {
                ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForFormOrDomainChange(_surface);
            }

            _selectedPlatformId = preset.PlatformId;
            if (_placePlatformId != null)
            {
                _placePlatformId.SetValueWithoutNotify(preset.PlatformId);
            }

            if (_placeSideId != null)
            {
                _placeSideId.SetValueWithoutNotify(preset.DefaultSideId);
            }

            if (_placeUnitId != null)
            {
                _placeUnitId.SetValueWithoutNotify(
                    ScenarioPlatformDomainCatalog.SuggestUnitId(preset.PlatformId, _unitIdSequence));
            }

            if (_platformList != null)
            {
                foreach (var child in _platformList.Children())
                {
                    if (child is Button button)
                    {
                        var selected = button.userData is ScenarioPlatformPreset p
                            && string.Equals(p.PlatformId, preset.PlatformId, StringComparison.OrdinalIgnoreCase);
                        button.EnableInClassList("scenario-map-platform-row--selected", selected);
                    }
                }
            }

            SetStatus(
                $"Selected {ScenarioPlatformDomainCatalog.DomainLabel(preset.DomainId)} platform " +
                $"'{preset.DisplayName}' ({preset.PlatformId}). Begin Place or Place + Commit.");
        }

        private void TryBeginPlaceUnit()
        {
            if (_surface == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            var latText = FieldValue(_placeLat, "57.0");
            var lonText = FieldValue(_placeLon, "20.0");
            if (!TryParseLatLon(latText, lonText, out var lat, out var lon, out var err))
            {
                SetStatus(err);
                return;
            }

            try
            {
                var unitId = FieldValue(_placeUnitId, "u-new").Trim();
                var platformId = FieldValue(_placePlatformId, "ffg").Trim();
                _surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
                {
                    Id = unitId,
                    SideId = FieldValue(_placeSideId, "blue").Trim(),
                    PlatformId = platformId,
                    Lat = lat,
                    Lon = lon,
                });
                SetStatus($"Place-unit gesture staged for '{unitId}' platform={platformId}. Commit to apply via bus.");
                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Begin place unit failed: {ex.Message}");
            }
        }

        private void TryCommitPlaceUnit()
        {
            if (_surface == null || _findings == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            try
            {
                var result = _surface.CommitPlaceUnit(save: true);
                if (result == null)
                {
                    SetStatus("No tentative unit staged. Use Begin Place Unit first.");
                    return;
                }

                _findings.RefreshImmediate();
                if (result.Ok)
                {
                    _unitIdSequence++;
                    SetStatus($"Place unit committed (editVersion={result.EditVersion}).");
                    // Bump suggested unit id for next placement in this domain.
                    var platformId = FieldValue(_placePlatformId, "ffg").Trim();
                    if (_placeUnitId != null)
                    {
                        _placeUnitId.SetValueWithoutNotify(
                            ScenarioPlatformDomainCatalog.SuggestUnitId(platformId, _unitIdSequence));
                    }
                }
                else
                {
                    SetStatus($"Place unit failed: {result.ErrorCode} — {result.ErrorMessage}");
                }

                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"Commit place unit failed: {ex.Message}");
            }
        }

        private void TryPlaceAndCommit()
        {
            if (_surface == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            TryBeginPlaceUnit();
            if (_surface?.TentativeUnit != null)
            {
                TryCommitPlaceUnit();
            }
        }

        private void TryCancelGesture()
        {
            if (_surface == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            _surface.CancelGesture();
            SetStatus("Gesture cancelled.");
            RefreshAllUi();
        }

        private void TryUpsertRpPolygon()
        {
            if (_surface == null || _findings == null)
            {
                SetStatus(NoSessionMessage);
                return;
            }

            if (!TryParseLatLon(FieldValue(_rpLat0), FieldValue(_rpLon0), out var lat0, out var lon0, out var err0))
            {
                SetStatus("V0: " + err0);
                return;
            }

            if (!TryParseLatLon(FieldValue(_rpLat1), FieldValue(_rpLon1), out var lat1, out var lon1, out var err1))
            {
                SetStatus("V1: " + err1);
                return;
            }

            if (!TryParseLatLon(FieldValue(_rpLat2), FieldValue(_rpLon2), out var lat2, out var lon2, out var err2))
            {
                SetStatus("V2: " + err2);
                return;
            }

            try
            {
                var geometry = new List<ScenarioWaypointDto>
                {
                    new() { Lat = lat0, Lon = lon0 },
                    new() { Lat = lat1, Lon = lon1 },
                    new() { Lat = lat2, Lon = lon2 },
                };

                var rpId = FieldValue(_rpId, "rp-zone").Trim();
                _surface.BeginDrawReferencePoint(new ScenarioReferencePointDto
                {
                    Id = rpId,
                    Type = "polygon",
                    Geometry = geometry,
                });

                var result = _surface.CommitReferencePoint(save: true);
                if (result == null)
                {
                    SetStatus("RP commit returned null (no tentative RP).");
                    return;
                }

                _findings.RefreshImmediate();
                if (result.Ok)
                {
                    SetStatus($"RP polygon '{rpId}' committed (editVersion={result.EditVersion}).");
                }
                else
                {
                    SetStatus($"RP commit failed: {result.ErrorCode} — {result.ErrorMessage}");
                }

                RefreshAllUi();
            }
            catch (Exception ex)
            {
                SetStatus($"RP upsert failed: {ex.Message}");
            }
        }

        private static bool TryParseLatLon(
            string latText,
            string lonText,
            out double lat,
            out double lon,
            out string error)
        {
            lat = 0;
            lon = 0;
            error = string.Empty;

            if (!double.TryParse(latText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out lat))
            {
                error = $"Invalid lat '{latText}'.";
                return false;
            }

            if (!double.TryParse(lonText, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out lon))
            {
                error = $"Invalid lon '{lonText}'.";
                return false;
            }

            return true;
        }

        private void DisposeSession()
        {
            _findings = null;
            _surface = null;
            if (_session != null)
            {
                _session.Dispose();
                _session = null;
            }
        }
    }
}
#endif
