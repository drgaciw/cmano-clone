// SE-UX: Unified Scenario Editor product chrome —
// top bar · Map | Mission Board (+ Events P2.3) · drawer/inspector · findings dock · Play/Sample gate.
// Hosts remain thin binders over ScenarioEditorShellProjection and BoundedAuthoringWorkflow (ADR-010/017).
#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectAegis.Unity.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioEditorShellHost : MonoBehaviour
    {
        private const string RootName = "scenario-editor-shell-root";
        private const string ModeTitleName = "scenario-editor-shell-mode-title";
        private const string StatusName = "scenario-editor-shell-status";
        private const string FindingsSummaryName = "scenario-editor-shell-findings-summary";
        private const string PlayGateName = "scenario-editor-shell-play-gate";
        private const string TabMapName = "scenario-editor-shell-tab-map";
        private const string TabMissionsName = "scenario-editor-shell-tab-missions";
        private const string TabEventsName = "scenario-editor-shell-tab-events";
        private const string TabsName = "scenario-editor-shell-tabs";
        private const string MapSlotName = "scenario-editor-shell-map-slot";
        private const string MapSummaryName = "scenario-editor-shell-map-summary";
        private const string MissionBoardSlotName = "scenario-editor-shell-mission-board-slot";
        private const string SlotHiddenClass = "scenario-editor-shell-slot--hidden";
        private const string MapRootClass = "scenario-editor-shell--map";
        private const string MissionBoardRootClass = "scenario-editor-shell--mission-board";
        private const string ScenarioTitleName = "scenario-editor-shell-scenario-title";
        private const string DirtyName = "scenario-editor-shell-dirty";
        private const string EditVersionName = "scenario-editor-shell-edit-version";
        private const string InspectorBodyName = "scenario-editor-shell-inspector-body";
        private const string FindingsListName = "scenario-editor-shell-findings-list";
        private const string FilterAllName = "scenario-editor-shell-filter-all";
        private const string FilterErrorsName = "scenario-editor-shell-filter-errors";
        private const string FilterWarningsName = "scenario-editor-shell-filter-warnings";
        private const string BtnLoadName = "scenario-editor-shell-btn-load";
        private const string BtnSaveName = "scenario-editor-shell-btn-save";
        private const string BtnUndoName = "scenario-editor-shell-btn-undo";
        private const string BtnRedoName = "scenario-editor-shell-btn-redo";
        private const string BtnPlayName = "scenario-editor-shell-btn-play";
        private const string BtnSampleName = "scenario-editor-shell-btn-sample";
        private const string DrawerTitleName = "scenario-editor-shell-drawer-title";

        [SerializeField] private VisualTreeAsset? panelAsset;
        [SerializeField] private StyleSheet? panelStyles;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private C2AccessibilityScalePercent accessibilityScale = C2AccessibilityScalePercent.OneHundred;
        [SerializeField] private bool reducedMotion = true;

        private UIDocument _document = null!;
        private Label? _modeTitle;
        private Label? _status;
        private Label? _findingsSummary;
        private Label? _playGate;
        private Label? _scenarioTitle;
        private Label? _dirty;
        private Label? _editVersion;
        private Label? _inspectorBody;
        private Label? _drawerTitle;
        private Button? _tabMap;
        private Button? _tabMissions;
        private Button? _tabEvents;
        private Button? _filterAll;
        private Button? _filterErrors;
        private Button? _filterWarnings;
        private Button? _btnLoad;
        private Button? _btnSave;
        private Button? _btnUndo;
        private Button? _btnRedo;
        private Button? _btnPlay;
        private Button? _btnSample;
        private VisualElement? _mapSlot;
        private Label? _mapSummary;
        private VisualElement? _missionBoardSlot;
        private VisualElement? _tabs;
        private ScrollView? _findingsList;
        private ScenarioEditorShellState _state = ScenarioEditorShellProjection.Bind();
        private VisualElement? _wiredRoot;
        private bool _wired;
        private Action? _loadRequested;
        private Action? _saveRequested;
        private Action? _undoRequested;
        private Action? _redoRequested;
        private Action? _playRequested;
        private Action? _sampleRequested;
        private BoundedAuthoringWorkflow? _workflow;
        private PlayEntrySession? _playEntry;
        private DelegationBridgeHost? _bridgeHost;
        private string? _defaultDraftPath;
        private string? _defaultBackendId;
        private bool _ownsWorkflow;
        private bool _workflowSubscribed;
        private BoundedPlayHandoffResult? _lastPlayHandoff;

        /// <summary>Current shell projection state (test / curator sync).</summary>
        public ScenarioEditorShellState State => _state;

        /// <summary>True when a <see cref="BoundedAuthoringWorkflow"/> is attached to this shell host.</summary>
        public bool IsAuthoringAttached => _workflow != null;

        /// <summary>True when the attached authoring workflow has an open scenario session.</summary>
        public bool HasOpenAuthoringSession => _workflow?.HasOpenSession == true;

        /// <summary>True when an undo snapshot is available in the attached authoring workflow.</summary>
        public bool CanUndoAuthoring => _workflow?.CanUndo == true;

        /// <summary>True when a redo snapshot is available in the attached authoring workflow.</summary>
        public bool CanRedoAuthoring => _workflow?.CanRedo == true;

        /// <summary>Count of ORBAT units currently projected on the map surface.</summary>
        public int AuthoringUnitCount => _workflow?.Surface?.Units.Count ?? 0;

        /// <summary>Count of authored missions in the open scenario session.</summary>
        public int AuthoringMissionCount => _workflow?.Session?.Editor.Missions.Count ?? 0;

        /// <summary>True when DRG-371 generic-only capability warnings are present (non-blocking).</summary>
        public bool HasGenericExecutionWarnings => _workflow?.Findings?.Gate.HasGenericExecutionWarnings == true;

        /// <summary>True when unsupported mission roles block Export and Play.</summary>
        public bool HasUnsupportedExecution => _workflow?.Findings?.Gate.HasUnsupportedExecution == true;

        /// <summary>True when the validation export gate allows Export and Play.</summary>
        public bool CanExportAuthoring => _workflow?.Findings?.Gate.CanExport == true;

        /// <summary>Most recent validated export artifact path, if any.</summary>
        public string? LastExportArtifactPath => _workflow?.LastExportOutcome?.ArtifactPath;

        /// <summary>Immutable state of the bound <see cref="PlayEntrySession"/>, if attached.</summary>
        public PlayEntryState? ActivePlayEntryState => _playEntry?.State;

        /// <summary>Most recent Play handoff result, if any.</summary>
        public BoundedPlayHandoffResult? LastPlayHandoff => _lastPlayHandoff;

        /// <summary>Simulation mode used when handing off from Edit to Play.</summary>
        public SimulationModeKind PlayHandoffMode { get; set; } = SimulationModeKind.Mixed;

        /// <summary>Commanded side used when handing off from Edit to Play.</summary>
        public PlaySide PlayHandoffSide { get; set; } = PlaySide.Friendly;

        /// <summary>Raised when Load is clicked (curator / session owns IO).</summary>
        public event Action? LoadRequested
        {
            add
            {
                _loadRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _loadRequested -= value;
                RefreshActionAvailability();
            }
        }

        /// <summary>Raised when Save is clicked (allowed with errors).</summary>
        public event Action? SaveRequested
        {
            add
            {
                _saveRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _saveRequested -= value;
                RefreshActionAvailability();
            }
        }

        public event Action? UndoRequested
        {
            add
            {
                _undoRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _undoRequested -= value;
                RefreshActionAvailability();
            }
        }
        public event Action? RedoRequested
        {
            add
            {
                _redoRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _redoRequested -= value;
                RefreshActionAvailability();
            }
        }

        /// <summary>Raised when Play is clicked and projection allows it.</summary>
        public event Action? PlayRequested
        {
            add
            {
                _playRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _playRequested -= value;
                RefreshActionAvailability();
            }
        }

        /// <summary>Raised when Sample is clicked and projection allows it.</summary>
        public event Action? SampleRequested
        {
            add
            {
                _sampleRequested += value;
                RefreshActionAvailability();
            }
            remove
            {
                _sampleRequested -= value;
                RefreshActionAvailability();
            }
        }

        /// <summary>Raised when a findings row is activated (entity jump already applied to state).</summary>
        public event Action<ScenarioEditorFindingRow>? FindingActivated;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (panelAsset != null)
            {
                _document.visualTreeAsset = panelAsset;
            }

            if (panelStyles != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.styleSheets.Add(panelStyles);
            }
        }

        private void OnEnable()
        {
            TryWireElements();
            ApplyState(_state);
            ApplyAccessibility();
            ApplyPanelVisibility();
        }

        private void OnDisable()
        {
            ResetWiredElements();
        }

        private void OnDestroy()
        {
            DetachAuthoringWorkflow();
        }

        /// <summary>
        /// Attaches a default <see cref="BoundedAuthoringWorkflow"/> and <see cref="PlayEntrySession"/>
        /// owned by this shell host (DRG-346).
        /// </summary>
        public void AttachDefaultAuthoringWorkflow(
            DelegationBridgeHost? bridgeHost = null,
            string? defaultDraftPath = null,
            string? defaultBackendId = null)
        {
            var workflow = new BoundedAuthoringWorkflow();
            var playCatalog = (bridgeHost != null ? bridgeHost.CatalogReader : null)
                ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
                ?? InMemoryCatalogReader.BalticPatrolFixture();
            var playEntry = new PlayEntrySession(catalog: playCatalog);
            AttachAuthoringWorkflow(
                workflow,
                playEntry,
                bridgeHost,
                defaultDraftPath,
                defaultBackendId,
                ownsWorkflow: true);
        }

        /// <summary>
        /// Attaches a caller-owned <see cref="BoundedAuthoringWorkflow"/> and optional
        /// <see cref="PlayEntrySession"/> / <see cref="DelegationBridgeHost"/> to the D26 shell actions.
        /// </summary>
        public void AttachAuthoringWorkflow(
            BoundedAuthoringWorkflow workflow,
            PlayEntrySession? playEntry = null,
            DelegationBridgeHost? bridgeHost = null,
            string? defaultDraftPath = null,
            string? defaultBackendId = null)
        {
            AttachAuthoringWorkflow(
                workflow,
                playEntry,
                bridgeHost,
                defaultDraftPath,
                defaultBackendId,
                ownsWorkflow: false);
        }

        /// <summary>Detaches the current authoring workflow, restoring D26 NOT_AVAILABLE gating.</summary>
        public void DetachAuthoringWorkflow()
        {
            if (_workflowSubscribed)
            {
                LoadRequested -= HandleLoadRequested;
                SaveRequested -= HandleSaveRequested;
                UndoRequested -= HandleUndoRequested;
                RedoRequested -= HandleRedoRequested;
                PlayRequested -= HandlePlayRequested;
                SampleRequested -= HandleSampleRequested;
                _workflowSubscribed = false;
            }

            if (_ownsWorkflow)
            {
                _workflow?.Dispose();
            }

            _workflow = null;
            _playEntry = null;
            _bridgeHost = null;
            _defaultDraftPath = null;
            _defaultBackendId = null;
            _ownsWorkflow = false;
            _lastPlayHandoff = null;
            _state = ScenarioEditorShellProjection.Bind(mode: _state.Mode, findingsFilter: _state.FindingsFilter);
            if (_document != null)
            {
                ApplyState(_state);
            }
        }

        /// <summary>Opens a scenario draft through the attached workflow and refreshes shell chrome.</summary>
        public bool OpenAuthoringDraft(string scenarioPath, string? backendId = null)
        {
            if (_workflow == null)
            {
                return false;
            }

            var resolvedBackend = string.IsNullOrWhiteSpace(backendId)
                ? (_defaultBackendId ?? MissionRoleCapabilityManifest.DefaultBackendId)
                : backendId.Trim();
            var result = _workflow.OpenDraft(scenarioPath, backendId: resolvedBackend);
            if (result.Succeeded
                && _workflow.HasOpenSession
                && _workflow.BoundCatalog == null
                && _workflow.Session != null)
            {
                var meta = _workflow.Session.Editor.Metadata;
                var dbRef = !string.IsNullOrWhiteSpace(meta.DbRef)
                    ? meta.DbRef.Trim()
                    : meta.DbSnapshotId?.Trim();
                if (!string.IsNullOrWhiteSpace(dbRef)
                    && CatalogValidationDefaults.TryResolveBalticDbRef(dbRef, out _))
                {
                    var fallbackCatalog = (_bridgeHost != null ? _bridgeHost.CatalogReader : null)
                        ?? InMemoryCatalogReader.BalticPatrolFixture();
                    result = _workflow.OpenDraft(scenarioPath, catalog: fallbackCatalog, backendId: resolvedBackend);
                }
            }

            if (result.Succeeded)
            {
                _defaultDraftPath = scenarioPath;
                _lastPlayHandoff = null;
                SyncFromWorkflow();
                return true;
            }

            SyncFromWorkflow();
            SetStatusSummary(result.Message);
            return false;
        }

        /// <summary>Selects an ORBAT unit on the map surface and updates inspector/map slot chrome.</summary>
        public bool SelectAuthoringUnit(string? unitId)
        {
            if (_workflow == null)
            {
                return false;
            }

            var result = _workflow.SelectUnit(unitId);
            SyncFromWorkflow();
            return result.Succeeded;
        }

        /// <summary>Places or edits a unit on the map surface via the attached workflow.</summary>
        public BoundedAuthoringOperationResult PlaceOrEditAuthoringUnit(BoundedUnitEditRequest request)
        {
            if (_workflow == null)
            {
                return BoundedAuthoringOperationResult.Fail("NOT_ATTACHED", "Authoring workflow is not attached.");
            }

            var result = _workflow.PlaceOrEditUnit(request);
            SyncFromWorkflow();
            return result;
        }

        /// <summary>Assigns an existing unit to a mission through the attached workflow.</summary>
        public BoundedAuthoringOperationResult AssignAuthoringUnitToMission(
            string missionId,
            string unitId,
            bool saveImmediately = false)
        {
            if (_workflow == null)
            {
                return BoundedAuthoringOperationResult.Fail("NOT_ATTACHED", "Authoring workflow is not attached.");
            }

            var result = _workflow.AssignUnitToMission(missionId, unitId, saveImmediately);
            SyncFromWorkflow();
            return result;
        }

        /// <summary>Removes an authored mission through the attached workflow.</summary>
        public BoundedAuthoringOperationResult RemoveAuthoringMission(
            string missionId,
            bool saveImmediately = false)
        {
            if (_workflow == null)
            {
                return BoundedAuthoringOperationResult.Fail("NOT_ATTACHED", "Authoring workflow is not attached.");
            }

            var result = _workflow.RemoveMission(missionId, saveImmediately);
            SyncFromWorkflow();
            return result;
        }

        /// <summary>Reads a projected unit's fields from the map surface without exposing Data DTOs.</summary>
        public bool TryGetAuthoringUnit(
            string unitId,
            out string sideId,
            out string platformId,
            out double lat,
            out double lon)
        {
            sideId = string.Empty;
            platformId = string.Empty;
            lat = 0;
            lon = 0;

            var units = _workflow?.Surface?.Units;
            if (units == null || string.IsNullOrWhiteSpace(unitId))
            {
                return false;
            }

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (string.Equals(unit.UnitId, unitId, StringComparison.OrdinalIgnoreCase))
                {
                    sideId = unit.SideId;
                    platformId = unit.PlatformId;
                    lat = unit.Lat;
                    lon = unit.Lon;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Validates the open draft and refreshes findings/gate chrome.</summary>
        public bool ValidateAuthoring(string? backendId = null)
        {
            if (_workflow == null || !_workflow.HasOpenSession)
            {
                return false;
            }

            _workflow.Validate(backendId);
            SyncFromWorkflow();
            return true;
        }

        /// <summary>Saves the current draft (allowed even with validation errors) and refreshes chrome.</summary>
        public bool SaveAuthoringDraft()
        {
            if (_workflow == null || !_workflow.HasOpenSession)
            {
                return false;
            }

            var outcome = _workflow.SaveDraft();
            SyncFromWorkflow();
            return outcome.Saved;
        }

        /// <summary>Undoes the most recent authoring edit and refreshes chrome.</summary>
        public bool UndoAuthoring()
        {
            if (_workflow == null)
            {
                return false;
            }

            var result = _workflow.Undo();
            SyncFromWorkflow();
            return result.Succeeded;
        }

        /// <summary>Redoes the most recently undone authoring edit and refreshes chrome.</summary>
        public bool RedoAuthoring()
        {
            if (_workflow == null)
            {
                return false;
            }

            var result = _workflow.Redo();
            SyncFromWorkflow();
            return result.Succeeded;
        }

        /// <summary>Exports the validated package and hands off into <see cref="PlayEntrySession"/>.</summary>
        public BoundedPlayHandoffResult ExportAndPlayAuthoring()
        {
            if (_workflow == null || _playEntry == null)
            {
                var unattached = BoundedPlayHandoffResult.Fail("NOT_ATTACHED", "Authoring workflow is not attached.");
                _lastPlayHandoff = unattached;
                return unattached;
            }

            var handoff = _workflow.HandOffToPlay(_playEntry, PlayHandoffMode, PlayHandoffSide);
            _lastPlayHandoff = handoff;
            if (handoff.Succeeded && _bridgeHost != null && _playEntry.Bridge != null)
            {
                var seed = unchecked((int)(_workflow.Session?.Editor.Metadata.Seed ?? 42UL));
                _bridgeHost.BindExternalSession(_playEntry.Bridge, seed);
            }

            SyncFromWorkflow();
            return handoff;
        }

        /// <summary>Synchronizes shell projection state and map slot summary from the attached workflow.</summary>
        public void SyncFromWorkflow()
        {
            if (_workflow == null)
            {
                return;
            }

            _state = _workflow.BuildShellState(_state.Mode, _state.FindingsFilter);
            if (_document != null)
            {
                ApplyState(_state);
            }
        }

        private void AttachAuthoringWorkflow(
            BoundedAuthoringWorkflow workflow,
            PlayEntrySession? playEntry,
            DelegationBridgeHost? bridgeHost,
            string? defaultDraftPath,
            string? defaultBackendId,
            bool ownsWorkflow)
        {
            if (workflow == null)
            {
                throw new ArgumentNullException(nameof(workflow));
            }

            DetachAuthoringWorkflow();

            _workflow = workflow;
            _playEntry = playEntry ?? new PlayEntrySession();
            _bridgeHost = bridgeHost;
            _defaultDraftPath = defaultDraftPath;
            _defaultBackendId = string.IsNullOrWhiteSpace(defaultBackendId) ? null : defaultBackendId.Trim();
            _ownsWorkflow = ownsWorkflow;

            LoadRequested += HandleLoadRequested;
            SaveRequested += HandleSaveRequested;
            UndoRequested += HandleUndoRequested;
            RedoRequested += HandleRedoRequested;
            PlayRequested += HandlePlayRequested;
            SampleRequested += HandleSampleRequested;
            _workflowSubscribed = true;

            SyncFromWorkflow();
        }

        private void HandleLoadRequested()
        {
            var path = !string.IsNullOrWhiteSpace(_defaultDraftPath)
                ? _defaultDraftPath
                : _workflow?.Session?.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                SetStatusSummary("No scenario path configured for Load.");
                return;
            }

            OpenAuthoringDraft(path!, _defaultBackendId);
        }

        private void HandleSaveRequested()
        {
            SaveAuthoringDraft();
        }

        private void HandleUndoRequested()
        {
            UndoAuthoring();
        }

        private void HandleRedoRequested()
        {
            RedoAuthoring();
        }

        private void HandlePlayRequested()
        {
            ExportAndPlayAuthoring();
        }

        private void HandleSampleRequested()
        {
            if (_workflow == null || !_workflow.HasOpenSession)
            {
                return;
            }

            _workflow.ExportPackage();
            SyncFromWorkflow();
        }

        /// <summary>Switch Map | Mission Board without destroying child slot content.</summary>
        public void SetMode(ScenarioEditorShellMode mode)
        {
            _state = ScenarioEditorShellProjection.WithMode(_state, mode);
            ApplyState(_state);
        }

        public void SetSelectedEntityId(string? entityId)
        {
            _state = ScenarioEditorShellProjection.WithSelection(_state, entityId);
            ApplyState(_state);
        }

        public void SetStatusSummary(string statusSummary)
        {
            _state = ScenarioEditorShellProjection.WithStatus(_state, statusSummary);
            ApplyState(_state);
        }

        /// <summary>Refresh live findings counts and Play/Sample gate (error-severity only).</summary>
        public void SetFindings(int errorCount, int warningCount)
        {
            _state = ScenarioEditorShellProjection.WithFindings(_state, errorCount, warningCount);
            ApplyState(_state);
        }

        /// <summary>Bind top-bar temporal/file chrome (title, dirty, editVersion, session, undo stack).</summary>
        public void SetTopBar(
            string scenarioTitle,
            bool isDirty,
            int editVersion,
            bool sessionOpen,
            bool undoEnabled = false,
            bool redoEnabled = false)
        {
            _state = ScenarioEditorShellProjection.WithTopBar(
                _state,
                scenarioTitle,
                isDirty,
                editVersion,
                sessionOpen,
                undoEnabled,
                redoEnabled);
            ApplyState(_state);
        }

        /// <summary>Replace findings dock rows and optional filter; updates counts + Play gate.</summary>
        public void SetFindingRows(
            IReadOnlyList<ScenarioEditorFindingRow> rows,
            ScenarioEditorFindingsFilter? filter = null)
        {
            _state = ScenarioEditorShellProjection.WithFindingRows(_state, rows, filter);
            ApplyState(_state);
        }

        public void SetFindingsFilter(ScenarioEditorFindingsFilter filter)
        {
            _state = ScenarioEditorShellProjection.WithFindingsFilter(_state, filter);
            ApplyState(_state);
        }

        /// <summary>Jump selection (and mode when mission-preferring) from a findings row.</summary>
        public void ActivateFinding(ScenarioEditorFindingRow row)
        {
            _state = ScenarioEditorShellProjection.JumpToFinding(_state, row);
            ApplyState(_state);
            FindingActivated?.Invoke(row);
        }

        /// <summary>Subscribers change action availability (UAT D26), so re-apply the current state.</summary>
        private void RefreshActionAvailability()
        {
            if (_document != null)
            {
                ApplyState(_state);
            }
        }

        private void TryWireElements()
        {
            if (_document == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            if (_wiredRoot != null && (!ReferenceEquals(_wiredRoot, root) || (_btnLoad != null && root.Q<Button>(BtnLoadName) != _btnLoad)))
            {
                ResetWiredElements();
            }

            _wiredRoot = root;

            _modeTitle ??= root.Q<Label>(ModeTitleName);
            _status ??= root.Q<Label>(StatusName);
            _findingsSummary ??= root.Q<Label>(FindingsSummaryName);
            _playGate ??= root.Q<Label>(PlayGateName);
            _scenarioTitle ??= root.Q<Label>(ScenarioTitleName);
            _dirty ??= root.Q<Label>(DirtyName);
            _editVersion ??= root.Q<Label>(EditVersionName);
            _inspectorBody ??= root.Q<Label>(InspectorBodyName);
            _drawerTitle ??= root.Q<Label>(DrawerTitleName);
            _mapSlot ??= root.Q(MapSlotName);
            if (_mapSlot != null && _mapSummary == null)
            {
                _mapSummary = _mapSlot.Q<Label>(MapSummaryName)
                              ?? _mapSlot.Q<Label>(className: "scenario-editor-shell-placeholder");
                if (_mapSummary == null)
                {
                    _mapSummary = new Label("Map Authoring surface slot") { name = MapSummaryName };
                    _mapSummary.AddToClassList("scenario-editor-shell-placeholder");
                    _mapSlot.Add(_mapSummary);
                }
            }

            _missionBoardSlot ??= root.Q(MissionBoardSlotName);
            _findingsList ??= root.Q<ScrollView>(FindingsListName);

            if (_tabMap == null)
            {
                _tabMap = root.Q<Button>(TabMapName);
                if (_tabMap != null)
                {
                    _tabMap.clicked += OnTabMapClicked;
                }
            }

            if (_tabMissions == null)
            {
                _tabMissions = root.Q<Button>(TabMissionsName);
                if (_tabMissions != null)
                {
                    _tabMissions.clicked += OnTabMissionsClicked;
                }
            }

            _tabEvents ??= root.Q<Button>(TabEventsName);
            if (_tabEvents != null)
            {
                _tabEvents.SetEnabled(false);
            }

            WireFilter(root, ref _filterAll, FilterAllName, OnFilterAllClicked);
            WireFilter(root, ref _filterErrors, FilterErrorsName, OnFilterErrorsClicked);
            WireFilter(root, ref _filterWarnings, FilterWarningsName, OnFilterWarningsClicked);

            WireAction(root, ref _btnLoad, BtnLoadName, OnBtnLoadClicked);
            WireAction(root, ref _btnSave, BtnSaveName, OnBtnSaveClicked);
            WireAction(root, ref _btnUndo, BtnUndoName, OnBtnUndoClicked);
            WireAction(root, ref _btnRedo, BtnRedoName, OnBtnRedoClicked);
            WireAction(root, ref _btnPlay, BtnPlayName, OnBtnPlayClicked);
            WireAction(root, ref _btnSample, BtnSampleName, OnBtnSampleClicked);

            if (_tabs == null)
            {
                _tabs = root.Q(TabsName);
                if (_tabs != null)
                {
                    _tabs.RegisterCallback<KeyDownEvent>(OnTabsKeyDown);
                }
            }

            _wired = root.Q(RootName) != null;
        }

        private void ResetWiredElements()
        {
            if (_tabMap != null)
            {
                _tabMap.clicked -= OnTabMapClicked;
                _tabMap = null;
            }

            if (_tabMissions != null)
            {
                _tabMissions.clicked -= OnTabMissionsClicked;
                _tabMissions = null;
            }

            if (_filterAll != null)
            {
                _filterAll.clicked -= OnFilterAllClicked;
                _filterAll = null;
            }

            if (_filterErrors != null)
            {
                _filterErrors.clicked -= OnFilterErrorsClicked;
                _filterErrors = null;
            }

            if (_filterWarnings != null)
            {
                _filterWarnings.clicked -= OnFilterWarningsClicked;
                _filterWarnings = null;
            }

            if (_btnLoad != null)
            {
                _btnLoad.clicked -= OnBtnLoadClicked;
                _btnLoad = null;
            }

            if (_btnSave != null)
            {
                _btnSave.clicked -= OnBtnSaveClicked;
                _btnSave = null;
            }

            if (_btnUndo != null)
            {
                _btnUndo.clicked -= OnBtnUndoClicked;
                _btnUndo = null;
            }

            if (_btnRedo != null)
            {
                _btnRedo.clicked -= OnBtnRedoClicked;
                _btnRedo = null;
            }

            if (_btnPlay != null)
            {
                _btnPlay.clicked -= OnBtnPlayClicked;
                _btnPlay = null;
            }

            if (_btnSample != null)
            {
                _btnSample.clicked -= OnBtnSampleClicked;
                _btnSample = null;
            }

            if (_tabs != null)
            {
                _tabs.UnregisterCallback<KeyDownEvent>(OnTabsKeyDown);
                _tabs = null;
            }

            _modeTitle = null;
            _status = null;
            _findingsSummary = null;
            _playGate = null;
            _scenarioTitle = null;
            _dirty = null;
            _editVersion = null;
            _inspectorBody = null;
            _drawerTitle = null;
            _mapSlot = null;
            _mapSummary = null;
            _missionBoardSlot = null;
            _findingsList = null;
            _tabEvents = null;
            _wiredRoot = null;
            _wired = false;
        }

        private void OnTabMapClicked() => SetMode(ScenarioEditorShellMode.Map);
        private void OnTabMissionsClicked() => SetMode(ScenarioEditorShellMode.MissionBoard);
        private void OnFilterAllClicked() => SetFindingsFilter(ScenarioEditorFindingsFilter.All);
        private void OnFilterErrorsClicked() => SetFindingsFilter(ScenarioEditorFindingsFilter.Errors);
        private void OnFilterWarningsClicked() => SetFindingsFilter(ScenarioEditorFindingsFilter.Warnings);
        private void OnBtnLoadClicked() => _loadRequested?.Invoke();
        private void OnBtnSaveClicked() { if (_state.SaveEnabled) _saveRequested?.Invoke(); }
        private void OnBtnUndoClicked() { if (_state.UndoEnabled) _undoRequested?.Invoke(); }
        private void OnBtnRedoClicked() { if (_state.RedoEnabled) _redoRequested?.Invoke(); }
        private void OnBtnPlayClicked() { if (_state.PlayEnabled) _playRequested?.Invoke(); }
        private void OnBtnSampleClicked() { if (_state.SampleEnabled) _sampleRequested?.Invoke(); }

        private static void WireFilter(
            VisualElement root,
            ref Button? field,
            string name,
            Action onClick)
        {
            if (field != null)
            {
                return;
            }

            field = root.Q<Button>(name);
            if (field != null)
            {
                field.clicked += onClick;
            }
        }

        private static void WireAction(
            VisualElement root,
            ref Button? field,
            string name,
            Action onClick)
        {
            if (field != null)
            {
                return;
            }

            field = root.Q<Button>(name);
            if (field != null)
            {
                field.clicked += onClick;
            }
        }

        private void OnTabsKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.RightArrow or KeyCode.LeftArrow
                || (evt.keyCode == KeyCode.Tab && evt.ctrlKey))
            {
                _state = ScenarioEditorShellProjection.CycleMode(_state);
                ApplyState(_state);
                evt.StopPropagation();
            }
        }

        private void ApplyState(ScenarioEditorShellState state)
        {
            if (_document == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            if (!ReferenceEquals(_wiredRoot, root))
            {
                TryWireElements();
            }

            root.EnableInClassList(MapRootClass, state.Mode == ScenarioEditorShellMode.Map);
            root.EnableInClassList(MissionBoardRootClass, state.Mode == ScenarioEditorShellMode.MissionBoard);

            if (_modeTitle != null)
            {
                _modeTitle.text = state.ModeTitle;
            }

            if (_status != null)
            {
                _status.text = state.StatusSummary;
            }

            if (_scenarioTitle != null)
            {
                _scenarioTitle.text = state.ScenarioTitle;
            }

            if (_dirty != null)
            {
                _dirty.text = state.DirtyLabel;
                _dirty.EnableInClassList("scenario-editor-shell-dirty--unsaved", state.IsDirty);
            }

            if (_editVersion != null)
            {
                _editVersion.text = $"v{state.EditVersion}";
            }

            if (_findingsSummary != null)
            {
                _findingsSummary.text = state.FindingsSummaryText;
            }

            if (_playGate != null)
            {
                _playGate.text = state.PlayBlockReason;
                _playGate.style.display = state.PlayBlocked ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_inspectorBody != null)
            {
                _inspectorBody.text = state.SelectionInspectorText;
            }

            if (_drawerTitle != null)
            {
                _drawerTitle.text = state.Mode == ScenarioEditorShellMode.Map
                    ? "ORBAT / Zones"
                    : "Mission Filters";
            }

            _tabMap?.EnableInClassList("scenario-editor-shell-tab--active", state.MapTabActive);
            _tabMissions?.EnableInClassList("scenario-editor-shell-tab--active", state.MissionBoardTabActive);
            _tabEvents?.EnableInClassList("scenario-editor-shell-tab--disabled", !state.EventsTabEnabled);

            ApplyActionButton(_btnLoad, "Load", _loadRequested != null, state.LoadEnabled, null);
            ApplyActionButton(_btnSave, "Save", _saveRequested != null, state.SaveEnabled, null);
            ApplyActionButton(_btnUndo, "Undo", _undoRequested != null, state.UndoEnabled, null);
            ApplyActionButton(_btnRedo, "Redo", _redoRequested != null, state.RedoEnabled, null);
            ApplyActionButton(_btnPlay, "Play", _playRequested != null, state.PlayEnabled,
                state.PlayBlocked ? state.PlayBlockReason : null);
            ApplyActionButton(_btnSample, "Sample", _sampleRequested != null, state.SampleEnabled,
                state.PlayBlocked ? state.PlayBlockReason : null);

            ApplyFilterChip(_filterAll, ScenarioEditorFindingsFilter.All, state.FindingsFilter);
            ApplyFilterChip(_filterErrors, ScenarioEditorFindingsFilter.Errors, state.FindingsFilter);
            ApplyFilterChip(_filterWarnings, ScenarioEditorFindingsFilter.Warnings, state.FindingsFilter);

            _mapSlot?.EnableInClassList(SlotHiddenClass, !state.MapContentVisible);
            _missionBoardSlot?.EnableInClassList(SlotHiddenClass, !state.MissionBoardContentVisible);

            UpdateMapSlotSummary();
            RebuildFindingsList(state);
        }

        private void UpdateMapSlotSummary()
        {
            if (_mapSummary == null)
            {
                return;
            }

            if (_workflow == null || !_workflow.HasOpenSession || _workflow.Surface == null)
            {
                _mapSummary.text = "Map Authoring surface slot";
                return;
            }

            var sb = new StringBuilder();
            sb.Append(_state.ScenarioTitle)
              .Append(" (v").Append(_state.EditVersion)
              .Append(", backend=").Append(_workflow.BackendId).AppendLine(")");

            var units = _workflow.Surface.Units;
            sb.Append("Units (").Append(units.Count).Append("): ");
            for (var i = 0; i < units.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var u = units[i];
                sb.Append(u.UnitId).Append('[').Append(u.PlatformId).Append('@')
                  .Append(u.Lat.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(u.Lon.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(']');
            }

            sb.AppendLine();
            sb.Append("Selection: ").Append(_state.SelectedEntityId ?? "none")
              .Append(" | RPs: ").Append(_workflow.Surface.ReferencePoints.Count)
              .Append(" | Missions: ").Append(AuthoringMissionCount);

            var gate = _workflow.Findings?.Gate;
            if (gate != null && gate.HasUnsupportedExecution)
            {
                sb.AppendLine()
                  .Append("Capability: ")
                  .Append(gate.UnsupportedExecutionFindings.Count)
                  .Append(" unsupported role error(s) — Export and Play blocked.");
            }

            if (gate != null && gate.HasGenericExecutionWarnings)
            {
                sb.AppendLine()
                  .Append("Capability: ")
                  .Append(gate.GenericExecutionFindings.Count)
                  .Append(gate.CanExport
                      ? " generic-only warning(s) — Export and Play remain allowed."
                      : " generic-only warning(s) (non-blocking warning; Export and Play blocked by other errors).");
            }

            if (_lastPlayHandoff != null)
            {
                sb.AppendLine()
                  .Append("Play handoff: ")
                  .Append(_lastPlayHandoff.Succeeded ? _lastPlayHandoff.Message : _lastPlayHandoff.ErrorCode);
            }

            _mapSummary.text = sb.ToString();
        }

        private static void ApplyActionButton(
            Button? button,
            string actionName,
            bool hasSubscriber,
            bool stateEnabled,
            string? stateDisabledReason)
        {
            if (button == null)
            {
                return;
            }

            // UAT D26: unwired actions are disabled with a NOT_AVAILABLE reason.
            var availability = ScenarioEditorActionAvailability.Resolve(
                actionName, hasSubscriber, stateEnabled, stateDisabledReason);
            button.SetEnabled(availability.Enabled);
            button.tooltip = availability.Tooltip;
            button.EnableInClassList("scenario-editor-shell-action--disabled", !availability.Enabled);
        }

        private static void ApplyFilterChip(
            Button? button,
            ScenarioEditorFindingsFilter chip,
            ScenarioEditorFindingsFilter active)
        {
            if (button == null)
            {
                return;
            }

            button.EnableInClassList(
                "scenario-editor-shell-findings-filter--active",
                chip == active);
        }

        private void RebuildFindingsList(ScenarioEditorShellState state)
        {
            if (_findingsList == null)
            {
                return;
            }

            _findingsList.Clear();
            foreach (var row in state.VisibleFindingRows)
            {
                var rowEl = new VisualElement();
                rowEl.AddToClassList("scenario-editor-shell-finding-row");
                rowEl.AddToClassList(SeverityRowModifier(row.Severity));
                rowEl.focusable = true;

                var tag = new Label($"[{row.SeverityTag}]");
                tag.AddToClassList("scenario-editor-shell-finding-tag");
                tag.AddToClassList(SeverityTagModifier(row.Severity));

                var label = new Label($"{row.Code} — {row.Message}");
                label.AddToClassList("scenario-editor-shell-finding-label");

                rowEl.Add(tag);
                rowEl.Add(label);

                var captured = row;
                rowEl.RegisterCallback<ClickEvent>(_ => ActivateFinding(captured));
                rowEl.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space)
                    {
                        ActivateFinding(captured);
                        evt.StopPropagation();
                    }
                });

                _findingsList.Add(rowEl);
            }
        }

        private static string SeverityRowModifier(ScenarioEditorFindingSeverity severity) =>
            severity switch
            {
                ScenarioEditorFindingSeverity.Error => "scenario-editor-shell-finding-row--error",
                ScenarioEditorFindingSeverity.Warning => "scenario-editor-shell-finding-row--warning",
                _ => "scenario-editor-shell-finding-row--info",
            };

        private static string SeverityTagModifier(ScenarioEditorFindingSeverity severity) =>
            severity switch
            {
                ScenarioEditorFindingSeverity.Error => "scenario-editor-shell-finding-tag--error",
                ScenarioEditorFindingSeverity.Warning => "scenario-editor-shell-finding-tag--warning",
                _ => "scenario-editor-shell-finding-tag--info",
            };

        private void ApplyAccessibility()
        {
            if (_document == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var settings = new C2AccessibilitySettings(accessibilityScale, reducedMotion);
            root.EnableInClassList("aegis-scale-100", settings.ScalePercent == C2AccessibilityScalePercent.OneHundred);
            root.EnableInClassList("aegis-scale-125", settings.ScalePercent == C2AccessibilityScalePercent.OneTwentyFive);
            root.EnableInClassList("aegis-scale-150", settings.ScalePercent == C2AccessibilityScalePercent.OneFifty);
            root.EnableInClassList("reduced-motion", settings.ReducedMotion);
        }

        private void ApplyPanelVisibility()
        {
            if (_document == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root != null)
            {
                root.style.display = showPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
#endif
