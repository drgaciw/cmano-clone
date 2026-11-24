using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;

namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

/// <summary>
/// Headless bounded H4 authoring workflow façade (DRG-346 + DRG-371).
/// Coordinates <see cref="ScenarioAuthoringSession"/>, <see cref="MapAuthoringSurface"/>,
/// <see cref="LiveFindingsPresenter"/>, <see cref="EditModeController"/>, bounded undo/redo,
/// <see cref="ScenarioSaveExportGate"/>, and unidirectional handoff into <see cref="PlayEntrySession"/>.
/// Pure C# (zero <c>UnityEngine</c>, <c>netstandard2.1</c> compatible).
/// </summary>
public sealed class BoundedAuthoringWorkflow : IDisposable
{
    private readonly ValidationConfig _config;
    private readonly Stack<ScenarioDocumentDto> _redoStack = new();

    private ScenarioAuthoringSession? _session;
    private MapAuthoringSurface? _surface;
    private LiveFindingsPresenter? _findings;
    private EditModeController? _editMode;
    private ICatalogReader? _boundCatalog;
    private IDisposable? _ownedCatalog;
    private string _backendId = MissionRoleCapabilityManifest.DefaultBackendId;
    private IReadOnlyList<MissionRoleCapabilityRow> _capabilities = Array.Empty<MissionRoleCapabilityRow>();

    /// <summary>Creates a new bounded authoring workflow coordinator.</summary>
    public BoundedAuthoringWorkflow(ValidationConfig? config = null)
    {
        _config = config ?? new ValidationConfig();
    }

    /// <summary>True when a scenario draft is currently open.</summary>
    public bool HasOpenSession => _session != null;

    /// <summary>Active file-backed authoring session, or <c>null</c> when closed.</summary>
    public ScenarioAuthoringSession? Session => _session;

    /// <summary>Map surface bound to the open session.</summary>
    public MapAuthoringSurface? Surface => _surface;

    /// <summary>Live findings presenter bound to the open session and bound catalog.</summary>
    public LiveFindingsPresenter? Findings => _findings;

    /// <summary>Edit/Play gate controller bound to the open session.</summary>
    public EditModeController? EditMode => _editMode;

    /// <summary>Catalog reader bound to the open draft (null when unbound).</summary>
    public ICatalogReader? BoundCatalog => _boundCatalog;

    /// <summary>Execution backend id currently used for capability evaluation.</summary>
    public string BackendId => _backendId;

    /// <summary>True when at least one undo snapshot is available for the open draft.</summary>
    public bool CanUndo => _session != null && ScenarioUndoStackStore.Count(_session.Path) > 0;

    /// <summary>True when at least one redo snapshot is available in this session.</summary>
    public bool CanRedo => _session != null && _redoStack.Count > 0;

    /// <summary>Most recent draft save outcome, if any.</summary>
    public ScenarioDraftSaveOutcome? LastSaveOutcome { get; private set; }

    /// <summary>Most recent export outcome, if any.</summary>
    public ScenarioValidatedExportOutcome? LastExportOutcome { get; private set; }

    /// <summary>Most recent validation snapshot.</summary>
    public BoundedValidationResult? LastValidation { get; private set; }

    /// <summary>Most recent capability disclosure rows.</summary>
    public IReadOnlyList<MissionRoleCapabilityRow> Capabilities => _capabilities;

    /// <summary>Latest human-readable status line for the editor shell.</summary>
    public string LastStatusSummary { get; private set; } = "No scenario open.";

    /// <summary>
    /// Opens a scenario draft, binds <paramref name="catalog"/> (or resolves a catalog reader from
    /// metadata when <paramref name="autoResolveCatalogIfNull"/> is true, never falling back to Baltic
    /// fixtures when missing), rebuilds the map surface, and refreshes live findings.
    /// </summary>
    public BoundedAuthoringOperationResult OpenDraft(
        string scenarioPath,
        ICatalogReader? catalog = null,
        string backendId = MissionRoleCapabilityManifest.DefaultBackendId,
        bool autoResolveCatalogIfNull = true)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            return BoundedAuthoringOperationResult.Fail(
                "PATH_REQUIRED",
                "Scenario path is required.",
                _session?.EditVersion ?? 0,
                _findings?.LastReport,
                _findings?.Gate);
        }

        if (!File.Exists(scenarioPath))
        {
            return BoundedAuthoringOperationResult.Fail(
                "FILE_NOT_FOUND",
                $"Scenario file not found: {scenarioPath}",
                _session?.EditVersion ?? 0,
                _findings?.LastReport,
                _findings?.Gate);
        }

        ScenarioAuthoringSession nextSession;
        try
        {
            nextSession = ScenarioAuthoringSession.Open(scenarioPath);
        }
        catch (Exception ex)
        {
            return BoundedAuthoringOperationResult.Fail(
                "OPEN_FAILED",
                $"Failed to open scenario: {ex.Message}",
                _session?.EditVersion ?? 0,
                _findings?.LastReport,
                _findings?.Gate);
        }

        CloseCurrentSession();
        _session = nextSession;

        _backendId = string.IsNullOrWhiteSpace(backendId)
            ? MissionRoleCapabilityManifest.DefaultBackendId
            : backendId.Trim();

        if (catalog != null)
        {
            _boundCatalog = catalog;
            _ownedCatalog = null;
        }
        else if (autoResolveCatalogIfNull)
        {
            var resolved = TryResolveCatalogFromMetadata(_session.Editor.Metadata);
            _boundCatalog = resolved;
            _ownedCatalog = resolved as IDisposable;
        }
        else
        {
            _boundCatalog = null;
            _ownedCatalog = null;
        }

        _surface = new MapAuthoringSurface(_session);
        _surface.RebuildFromDocument();
        _findings = new LiveFindingsPresenter(_session, _boundCatalog, debounceMs: 0, _config, _backendId);
        _editMode = new EditModeController(_session, _findings);
        _redoStack.Clear();
        LastSaveOutcome = null;
        LastExportOutcome = null;

        var validation = Validate(_backendId);
        LastStatusSummary = $"Opened '{Path.GetFileName(scenarioPath)}'.";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>Selects an ORBAT unit by id on <see cref="Surface"/> (or clears selection when null/unknown).</summary>
    public BoundedAuthoringOperationResult SelectUnit(string? unitId)
    {
        if (_session == null || _surface == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        _surface.SelectUnit(unitId ?? string.Empty);
        var selected = _surface.Selection.SelectedUnitId;
        LastStatusSummary = string.IsNullOrEmpty(selected)
            ? "Selection cleared."
            : $"Selected unit '{selected}'.";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            _findings?.LastReport,
            _findings?.Gate);
    }

    /// <summary>
    /// Validates required unit fields (<see cref="BoundedUnitEditRequest.UnitId"/>,
    /// <see cref="BoundedUnitEditRequest.SideId"/>, <see cref="BoundedUnitEditRequest.PlatformId"/>,
    /// and finite coordinates), executes placement or edit through <see cref="ScenarioEditCommandBus"/>,
    /// optionally assigns to a mission, clears redo, rebuilds the map surface, and refreshes live findings.
    /// </summary>
    public BoundedAuthoringOperationResult PlaceOrEditUnit(BoundedUnitEditRequest? request)
    {
        if (_session == null || _surface == null || _findings == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        if (request == null)
        {
            return BoundedAuthoringOperationResult.Fail("REQUEST_REQUIRED", "Unit edit request is required.", _session.EditVersion);
        }

        if (string.IsNullOrWhiteSpace(request.UnitId))
        {
            return BoundedAuthoringOperationResult.Fail("UNIT_ID_REQUIRED", "Unit id is required.", _session.EditVersion);
        }

        if (string.IsNullOrWhiteSpace(request.SideId))
        {
            return BoundedAuthoringOperationResult.Fail("SIDE_ID_REQUIRED", "Side id is required.", _session.EditVersion);
        }

        if (string.IsNullOrWhiteSpace(request.PlatformId))
        {
            return BoundedAuthoringOperationResult.Fail("PLATFORM_ID_REQUIRED", "Platform id is required.", _session.EditVersion);
        }

        var latLonDiagnostic = ScenarioLatLonDiagnostics.Check(request.Lat, request.Lon);
        if (latLonDiagnostic != null)
        {
            return BoundedAuthoringOperationResult.Fail(latLonDiagnostic.Code, latLonDiagnostic.Message, _session.EditVersion);
        }

        var assignMissionId = string.IsNullOrWhiteSpace(request.AssignToMissionId)
            ? null
            : request.AssignToMissionId.Trim();
        if (assignMissionId != null &&
            !_session.Editor.Missions.Any(m => string.Equals(m.Id, assignMissionId, StringComparison.OrdinalIgnoreCase)))
        {
            return BoundedAuthoringOperationResult.Fail(
                "MISSION_NOT_FOUND",
                $"Mission id '{assignMissionId}' was not found.",
                _session.EditVersion,
                _findings.LastReport,
                _findings.Gate);
        }

        var unitId = request.UnitId.Trim();
        ScenarioOrbatUnitDto? existing = null;
        if (request.AllowReplace)
        {
            var existingUnits = _session.Editor.ToDto().Orbat?.Units;
            if (existingUnits != null)
            {
                for (var i = 0; i < existingUnits.Count; i++)
                {
                    if (string.Equals(existingUnits[i].Id, unitId, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = existingUnits[i];
                        break;
                    }
                }
            }
        }

        var dto = new ScenarioOrbatUnitDto
        {
            Id = unitId,
            SideId = request.SideId.Trim(),
            PlatformId = request.PlatformId.Trim(),
            PlatformDbId = existing?.PlatformDbId,
            Lat = request.Lat,
            Lon = request.Lon,
            BaseId = existing?.BaseId,
            GroupId = existing?.GroupId,
            ParentUnitId = existing?.ParentUnitId,
            RoeOverride = existing?.RoeOverride,
            EmconOverride = existing?.EmconOverride,
            DroneCount = existing?.DroneCount,
            HostUnitId = existing?.HostUnitId,
            MissionType = existing?.MissionType,
            Mode = existing?.Mode,
        };

        var mutation = request.AllowReplace
            ? _session.Bus.UpsertUnit(_session.EditVersion, dto, request.SaveImmediately)
            : _session.Bus.PlaceUnit(_session.EditVersion, dto, request.SaveImmediately);

        if (!mutation.Ok)
        {
            return BoundedAuthoringOperationResult.Fail(
                mutation.ErrorCode ?? "MUTATION_FAILED",
                mutation.ErrorMessage ?? "Unit mutation failed.",
                mutation.EditVersion,
                _findings.LastReport,
                _findings.Gate);
        }

        _redoStack.Clear();

        if (assignMissionId != null)
        {
            var assign = _session.Bus.AssignUnitToMission(
                _session.EditVersion,
                assignMissionId,
                dto.Id,
                request.SaveImmediately);
            if (!assign.Ok)
            {
                _session.Editor.PopUndo(_session.Path);
                if (request.SaveImmediately)
                {
                    _session.Save();
                }

                _surface.RebuildFromDocument();
                var rolledBackValidation = Validate(_backendId);
                return BoundedAuthoringOperationResult.Fail(
                    assign.ErrorCode ?? "MISSION_ASSIGN_FAILED",
                    assign.ErrorMessage ?? "Mission assignment failed.",
                    _session.EditVersion,
                    rolledBackValidation.Report,
                    rolledBackValidation.Gate);
            }
        }

        _surface.RebuildFromDocument();
        var validation = Validate(_backendId);
        LastStatusSummary = $"Applied unit '{dto.Id}' ({dto.PlatformId}).";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>Assigns an existing unit id to an existing mission through the command bus.</summary>
    public BoundedAuthoringOperationResult AssignUnitToMission(string missionId, string unitId, bool saveImmediately = false)
    {
        if (_session == null || _surface == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        var result = _session.Bus.AssignUnitToMission(_session.EditVersion, missionId, unitId, saveImmediately);
        if (!result.Ok)
        {
            return BoundedAuthoringOperationResult.Fail(
                result.ErrorCode ?? "ASSIGN_FAILED",
                result.ErrorMessage ?? "Failed to assign unit to mission.",
                result.EditVersion,
                _findings?.LastReport,
                _findings?.Gate);
        }

        _redoStack.Clear();
        _surface.RebuildFromDocument();
        var validation = Validate(_backendId);
        LastStatusSummary = $"Assigned '{unitId}' to mission '{missionId}'.";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>Removes an authored mission by id through the command bus.</summary>
    public BoundedAuthoringOperationResult RemoveMission(string missionId, bool saveImmediately = false)
    {
        if (_session == null || _surface == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        var result = _session.Bus.DeleteMission(_session.EditVersion, missionId, saveImmediately);
        if (!result.Ok)
        {
            return BoundedAuthoringOperationResult.Fail(
                result.ErrorCode ?? "DELETE_MISSION_FAILED",
                result.ErrorMessage ?? "Failed to remove mission.",
                result.EditVersion,
                _findings?.LastReport,
                _findings?.Gate);
        }

        _redoStack.Clear();
        _surface.RebuildFromDocument();
        var validation = Validate(_backendId);
        LastStatusSummary = $"Removed mission '{missionId}'.";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>Undoes the most recent committed mutation and pushes the pre-undo snapshot onto the redo stack.</summary>
    public BoundedAuthoringOperationResult Undo()
    {
        if (_session == null || _surface == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        if (!CanUndo)
        {
            return BoundedAuthoringOperationResult.Fail("NO_UNDO", "Nothing to undo.", _session.EditVersion, _findings?.LastReport, _findings?.Gate);
        }

        var preUndo = _session.Editor.CaptureUndoSnapshot();
        if (!_session.Editor.PopUndo(_session.Path))
        {
            return BoundedAuthoringOperationResult.Fail("NO_UNDO", "Nothing to undo.", _session.EditVersion, _findings?.LastReport, _findings?.Gate);
        }

        _session.Save();
        _redoStack.Push(preUndo);
        _surface.RebuildFromDocument();
        var validation = Validate(_backendId);
        LastStatusSummary = $"Undid edit (v{_session.EditVersion}).";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>Reapplies the most recently undone snapshot and pushes the pre-redo state back to the undo stack.</summary>
    public BoundedAuthoringOperationResult Redo()
    {
        if (_session == null || _surface == null)
        {
            return BoundedAuthoringOperationResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        if (_redoStack.Count == 0)
        {
            return BoundedAuthoringOperationResult.Fail("NO_REDO", "Nothing to redo.", _session.EditVersion, _findings?.LastReport, _findings?.Gate);
        }

        var preRedo = _session.Editor.CaptureUndoSnapshot();
        var target = _redoStack.Pop();
        _session.Editor.PersistUndoSnapshot(_session.Path, preRedo);
        _session.Editor.RestoreSnapshot(target);
        _session.Save();
        _surface.RebuildFromDocument();
        var validation = Validate(_backendId);
        LastStatusSummary = $"Redid edit (v{_session.EditVersion}).";
        return BoundedAuthoringOperationResult.Ok(
            LastStatusSummary,
            _session.EditVersion,
            validation.Report,
            validation.Gate);
    }

    /// <summary>
    /// Validates the open draft against <see cref="BoundCatalog"/> and <paramref name="backendId"/>
    /// (DRG-345 + DRG-371) and updates <see cref="Findings"/> and <see cref="Capabilities"/>.
    /// </summary>
    public BoundedValidationResult Validate(string? backendId = null)
    {
        if (_session == null || _findings == null)
        {
            throw new InvalidOperationException(ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        if (!string.IsNullOrWhiteSpace(backendId))
        {
            _backendId = backendId.Trim();
        }

        _findings.RefreshImmediate(_boundCatalog, _backendId);
        var report = _findings.LastReport!;
        var gate = _findings.Gate;
        _capabilities = MissionRoleCapabilityProjection.Project(_session.Editor.ToDto(), _backendId);
        LastValidation = new BoundedValidationResult(report, gate, _capabilities, _backendId);
        return LastValidation;
    }

    /// <summary>Saves the work-in-progress draft (ungated — allowed even when validation errors exist).</summary>
    public ScenarioDraftSaveOutcome SaveDraft()
    {
        if (_session == null)
        {
            throw new InvalidOperationException(ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        var outcome = ScenarioSaveExportGate.SaveDraft(_session, _boundCatalog, _config, _backendId);
        LastSaveOutcome = outcome;
        LastStatusSummary = outcome.StatusText;
        Validate(_backendId);
        return outcome;
    }

    /// <summary>
    /// Exports a validated artifact (<c>.export.json</c> by default). Blocked when Error findings exist;
    /// allowed when only <see cref="MissionRoleExecution.GenericOnly"/> Warnings exist.
    /// </summary>
    public ScenarioValidatedExportOutcome ExportPackage(string? exportPath = null)
    {
        if (_session == null)
        {
            throw new InvalidOperationException(ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        var validation = Validate(_backendId);
        if (_boundCatalog == null)
        {
            var blocking = validation.Report.Findings
                .Where(f => f.Severity >= _config.ExportBlockSeverityFloor)
                .ToArray();
            var unboundOutcome = new ScenarioValidatedExportOutcome(
                Allowed: false,
                ArtifactPath: null,
                Report: validation.Report,
                BlockingFindings: blocking,
                TransformCount: 0,
                StatusText: validation.Gate.BlockingReason ?? "Export blocked — no catalog bound.");
            LastExportOutcome = unboundOutcome;
            LastStatusSummary = unboundOutcome.StatusText;
            return unboundOutcome;
        }

        var targetPath = string.IsNullOrWhiteSpace(exportPath)
            ? ScenarioSaveExportGate.ExportArtifactPathFor(_session.Path)
            : exportPath;

        var outcome = ScenarioSaveExportGate.Export(
            _session.Editor.ToDto(),
            _boundCatalog,
            targetPath,
            _session.Path,
            _config,
            _backendId);
        LastExportOutcome = outcome;
        LastStatusSummary = outcome.StatusText;
        return outcome;
    }

    /// <summary>
    /// Enforces <see cref="EditModeController"/> + export gate, writes the validated export artifact,
    /// loads it into <paramref name="playEntry"/>, selects <paramref name="mode"/> and <paramref name="side"/>,
    /// and optionally begins execution.
    /// </summary>
    public BoundedPlayHandoffResult HandOffToPlay(
        PlayEntrySession playEntry,
        SimulationModeKind mode = SimulationModeKind.Mixed,
        PlaySide side = PlaySide.Friendly,
        IReadOnlyList<ICommandableTarget>? friendly = null,
        IReadOnlyList<ICommandableTarget>? opposing = null,
        string? exportPath = null,
        bool forceConfirmInvalid = false,
        bool beginExecution = true)
    {
        if (playEntry == null)
        {
            throw new ArgumentNullException(nameof(playEntry));
        }

        if (_session == null || _editMode == null)
        {
            return BoundedPlayHandoffResult.Fail("NO_SESSION", ScenarioMapAuthoringHostPolicy.NoSessionMessage);
        }

        Validate(_backendId);

        if (!_editMode.TryEnterPlay(forceConfirmInvalid))
        {
            var reason = _editMode.LastPlayBlockReason ?? "Play blocked by validation findings.";
            LastStatusSummary = reason;
            return BoundedPlayHandoffResult.Fail("PLAY_BLOCKED", reason);
        }

        var exported = ExportPackage(exportPath);
        if (!exported.Allowed || string.IsNullOrEmpty(exported.ArtifactPath))
        {
            _editMode.EnterEdit();
            LastStatusSummary = exported.StatusText;
            return BoundedPlayHandoffResult.Fail("EXPORT_BLOCKED", exported.StatusText);
        }

        var loadResult = playEntry.TryLoadFromPath(exported.ArtifactPath);
        if (!loadResult.Succeeded)
        {
            _editMode.EnterEdit();
            LastStatusSummary = loadResult.Message;
            return BoundedPlayHandoffResult.Fail(
                loadResult.ErrorCode ?? PlayEntryErrorCodes.SchemaError,
                loadResult.Message,
                exported.ArtifactPath,
                playEntry.State);
        }

        var modeResult = playEntry.TrySelectMode(mode);
        if (!modeResult.Succeeded)
        {
            _editMode.EnterEdit();
            LastStatusSummary = modeResult.Message;
            return BoundedPlayHandoffResult.Fail(
                modeResult.ErrorCode ?? PlayEntryErrorCodes.ModeRequired,
                modeResult.Message,
                exported.ArtifactPath,
                playEntry.State);
        }

        var sideResult = playEntry.TrySelectSide(side);
        if (!sideResult.Succeeded)
        {
            _editMode.EnterEdit();
            LastStatusSummary = sideResult.Message;
            return BoundedPlayHandoffResult.Fail(
                sideResult.ErrorCode ?? PlayEntryErrorCodes.SideRequired,
                sideResult.Message,
                exported.ArtifactPath,
                playEntry.State);
        }

        if (beginExecution)
        {
            var (resolvedFriendly, resolvedOpposing) = ResolveOrbatTargets(
                playEntry,
                friendly,
                opposing);
            var beginResult = playEntry.TryBeginExecution(resolvedFriendly, resolvedOpposing);
            if (!beginResult.Succeeded)
            {
                _editMode.EnterEdit();
                LastStatusSummary = beginResult.Message;
                return BoundedPlayHandoffResult.Fail(
                    beginResult.ErrorCode ?? PlayEntryErrorCodes.NotPlanning,
                    beginResult.Message,
                    exported.ArtifactPath,
                    playEntry.State);
            }
        }

        LastStatusSummary = $"Handed off '{Path.GetFileName(exported.ArtifactPath)}' to Play ({mode}, {side}).";
        return BoundedPlayHandoffResult.Ok(exported.ArtifactPath, playEntry.State, LastStatusSummary);
    }

    /// <summary>Builds <see cref="ScenarioEditorShellState"/> for <c>ScenarioEditorShellHost</c>.</summary>
    public ScenarioEditorShellState BuildShellState(
        ScenarioEditorShellMode mode = ScenarioEditorShellMode.Map,
        ScenarioEditorFindingsFilter filter = ScenarioEditorFindingsFilter.All)
    {
        if (_session == null || _findings == null)
        {
            return ScenarioEditorShellProjection.Bind(
                mode: mode,
                statusSummary: LastStatusSummary,
                sessionOpen: false,
                findingsFilter: filter);
        }

        var gate = _findings.Gate;
        var rows = gate.Findings
            .Select(f =>
            {
                var severity = f.Severity switch
                {
                    ValidationSeverity.Error => ScenarioEditorFindingSeverity.Error,
                    ValidationSeverity.Warning => ScenarioEditorFindingSeverity.Warning,
                    _ => ScenarioEditorFindingSeverity.Info,
                };
                var entityId = f.UnitId ?? f.MissionId ?? f.TargetId;
                var preferBoard = ScenarioEditorFindingsProjection.PreferMissionBoardForCode(f.Code);
                return ScenarioEditorFindingsProjection.CreateRow(
                    severity,
                    f.Code,
                    f.Message,
                    entityId,
                    preferBoard);
            })
            .ToArray();

        var selectedId = _surface?.Selection.SelectedUnitId ?? _surface?.Selection.SelectedReferencePointId;
        var title = string.IsNullOrWhiteSpace(_session.Editor.Metadata.Title)
            ? Path.GetFileNameWithoutExtension(_session.Path)
            : _session.Editor.Metadata.Title!;

        return ScenarioEditorShellProjection.Bind(
            mode: mode,
            selectedEntityId: selectedId,
            statusSummary: LastStatusSummary,
            errorFindingCount: gate.ErrorCount,
            warningFindingCount: gate.WarningCount,
            scenarioTitle: title,
            isDirty: _session.IsDirty,
            editVersion: _session.EditVersion,
            sessionOpen: true,
            undoEnabled: CanUndo,
            redoEnabled: CanRedo,
            findingsFilter: filter,
            findingRows: rows);
    }

    /// <summary>Builds <see cref="EditorSaveExportState"/> for Save-draft vs Export-validated chrome.</summary>
    public EditorSaveExportState BuildSaveExportState(ScenarioEditorShellState? shellState = null)
    {
        var shell = shellState ?? BuildShellState();
        return EditorSaveExportProjection.FromShell(shell, LastSaveOutcome, LastExportOutcome);
    }

    /// <summary>Builds the combined shell chrome, save/export state, gate state, and role capability rows.</summary>
    public BoundedShellSnapshot BuildShellSnapshot(
        ScenarioEditorShellMode mode = ScenarioEditorShellMode.Map,
        ScenarioEditorFindingsFilter filter = ScenarioEditorFindingsFilter.All)
    {
        var shell = BuildShellState(mode, filter);
        var saveExport = BuildSaveExportState(shell);
        var gate = _findings?.Gate ?? ScenarioExportGateState.Empty;
        return new BoundedShellSnapshot(shell, saveExport, gate, _capabilities);
    }

    /// <inheritdoc />
    public void Dispose() => CloseCurrentSession();

    private void CloseCurrentSession()
    {
        _redoStack.Clear();
        _session?.Dispose();
        _session = null;
        _surface = null;
        _findings = null;
        _editMode = null;
        _ownedCatalog?.Dispose();
        _ownedCatalog = null;
        _boundCatalog = null;
        _capabilities = Array.Empty<MissionRoleCapabilityRow>();
        LastValidation = null;
    }

    private static ICatalogReader? TryResolveCatalogFromMetadata(ScenarioMetadataDto metadata)
    {
        var dbRef = !string.IsNullOrWhiteSpace(metadata.DbRef)
            ? metadata.DbRef.Trim()
            : !string.IsNullOrWhiteSpace(metadata.DbSnapshotId)
                ? metadata.DbSnapshotId.Trim()
                : null;

        if (string.IsNullOrWhiteSpace(dbRef))
        {
            return null;
        }

        if (CatalogValidationDefaults.TryResolvePublicCorpusDbRef(dbRef, out _))
        {
            return CatalogReaderFactory.TryCreatePublicCorpusReader();
        }

        if (CatalogValidationDefaults.TryResolveBalticDbRef(dbRef, out _))
        {
            return CatalogReaderFactory.TryCreateBalticPatrolReader();
        }

        return null;
    }

    private static (IReadOnlyList<ICommandableTarget> Friendly, IReadOnlyList<ICommandableTarget> Opposing) ResolveOrbatTargets(
        PlayEntrySession playEntry,
        IReadOnlyList<ICommandableTarget>? friendly,
        IReadOnlyList<ICommandableTarget>? opposing)
    {
        if (friendly != null && opposing != null)
        {
            return (friendly, opposing);
        }

        var bridge = playEntry.Bridge!;
        var friendlyList = new List<ICommandableTarget>();
        var opposingList = new List<ICommandableTarget>();
        var units = playEntry.Document?.Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>();

        var entityCounter = 1;
        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.Id))
            {
                continue;
            }

            var registered = bridge.Registry.RegisterUnit(new EntityKey(entityCounter++), unit.Id);
            if (IsOpposingSide(unit.SideId))
            {
                opposingList.Add(registered.Target);
            }
            else
            {
                friendlyList.Add(registered.Target);
            }
        }

        return (
            friendly ?? friendlyList.ToArray(),
            opposing ?? opposingList.ToArray());
    }

    private static bool IsOpposingSide(string? sideId)
    {
        if (string.IsNullOrWhiteSpace(sideId))
        {
            return false;
        }

        return sideId.Contains("red", StringComparison.OrdinalIgnoreCase)
               || sideId.Contains("opposing", StringComparison.OrdinalIgnoreCase)
               || sideId.Contains("hostile", StringComparison.OrdinalIgnoreCase);
    }
}
