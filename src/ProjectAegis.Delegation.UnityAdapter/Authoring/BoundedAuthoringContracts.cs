namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using Data.Scenario.Authoring;
using Data.Validation;
using Delegation.Projection;
using PlayEntry;

/// <summary>Bounded unit place/edit request for <see cref="BoundedAuthoringWorkflow.PlaceOrEditUnit"/> (DRG-346).</summary>
public sealed class BoundedUnitEditRequest
{
    /// <summary>Unique ORBAT unit identifier (required).</summary>
    public string UnitId { get; init; } = "";

    /// <summary>Owning side identifier (required).</summary>
    public string SideId { get; init; } = "";

    /// <summary>Catalog platform identifier (required).</summary>
    public string PlatformId { get; init; } = "";

    /// <summary>Latitude in degrees [-90, 90].</summary>
    public double Lat { get; init; }

    /// <summary>Longitude in degrees [-180, 180].</summary>
    public double Lon { get; init; }

    /// <summary>True to update an existing unit with the same id; false to enforce place-new semantics.</summary>
    public bool AllowReplace { get; init; } = true;

    /// <summary>True to immediately persist the draft file on mutation; false leaves draft dirty until <see cref="BoundedAuthoringWorkflow.SaveDraft"/>.</summary>
    public bool SaveImmediately { get; init; }

    /// <summary>Optional mission id to assign this unit to after placement/edit.</summary>
    public string? AssignToMissionId { get; init; }
}

/// <summary>Outcome of a bounded authoring step (open, select, place/edit, undo/redo, assign, remove).</summary>
public sealed record BoundedAuthoringOperationResult(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    int EditVersion = 0,
    ValidationReport? ValidationReport = null,
    ScenarioExportGateState? Gate = null)
{
    public static BoundedAuthoringOperationResult Ok(
        string message,
        int editVersion = 0,
        ValidationReport? report = null,
        ScenarioExportGateState? gate = null) =>
        new(true, null, message, editVersion, report, gate);

    public static BoundedAuthoringOperationResult Fail(
        string errorCode,
        string message,
        int editVersion = 0,
        ValidationReport? report = null,
        ScenarioExportGateState? gate = null) =>
        new(false, errorCode, message, editVersion, report, gate);
}

/// <summary>Combined validation, export-gate, and role-capability snapshot for a backend (DRG-345 + DRG-371 + DRG-346).</summary>
public sealed record BoundedValidationResult(
    ValidationReport Report,
    ScenarioExportGateState Gate,
    IReadOnlyList<MissionRoleCapabilityRow> Capabilities,
    string BackendId);

/// <summary>Outcome of handing off an authored scenario from Edit Mode into <see cref="PlayEntrySession"/> (DRG-346).</summary>
public sealed record BoundedPlayHandoffResult(
    bool Succeeded,
    string? ErrorCode,
    string Message,
    string? ExportArtifactPath,
    PlayEntryState? PlayState)
{
    public static BoundedPlayHandoffResult Ok(
        string exportArtifactPath,
        PlayEntryState playState,
        string message) =>
        new(true, null, message, exportArtifactPath, playState);

    public static BoundedPlayHandoffResult Fail(
        string errorCode,
        string message,
        string? exportArtifactPath = null,
        PlayEntryState? playState = null) =>
        new(false, errorCode, message, exportArtifactPath, playState);
}

/// <summary>Combined shell chrome, save/export state, gate state, and role-capability rows for thin UI hosts.</summary>
public sealed record BoundedShellSnapshot(
    ScenarioEditorShellState ShellState,
    EditorSaveExportState SaveExportState,
    ScenarioExportGateState Gate,
    IReadOnlyList<MissionRoleCapabilityRow> Capabilities);
