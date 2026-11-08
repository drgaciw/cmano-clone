namespace ProjectAegis.Delegation.Projection;

using Data.Scenario.Authoring;

/// <summary>Bindable Save-draft vs Export-validated chrome for the Mission Editor top bar.</summary>
public sealed record EditorSaveExportState(
    string SaveLabel,
    string SaveTooltip,
    bool SaveEnabled,
    string SaveUssClass,
    string SaveStatusText,
    string ExportLabel,
    string ExportTooltip,
    bool ExportEnabled,
    bool ExportBlocked,
    string ExportUssClass,
    string ExportStatusText,
    int BlockingFindingCount,
    string? LastExportArtifactPath);

/// <summary>
/// S125-04 / AUTH-04 (AME-6.5 / AC-12): Save and Export are different verbs with different gates.
/// Save persists the draft and stays enabled while invalid; Export writes a separate validated artifact
/// and is disabled while blocking findings exist. Pure projection — hosts call
/// <see cref="ScenarioSaveExportGate"/> and feed the outcomes back in.
/// </summary>
public static class EditorSaveExportProjection
{
    public const string SaveLabel = "Save draft";
    public const string ExportLabel = "Export validated";

    public const string SaveTooltip =
        "Writes the work-in-progress draft. Allowed with blocking findings; does not export.";

    public const string ExportTooltip =
        "Runs the validation export gate and writes a separate .export.json artifact only when it passes.";

    public static EditorSaveExportState Bind(
        bool sessionOpen,
        bool isDirty,
        int blockingFindingCount,
        ScenarioDraftSaveOutcome? lastSave = null,
        ScenarioValidatedExportOutcome? lastExport = null)
    {
        var blocking = Math.Max(0, blockingFindingCount);
        var exportBlocked = sessionOpen && blocking > 0;
        var exportEnabled = sessionOpen && blocking == 0;

        return new EditorSaveExportState(
            SaveLabel: SaveLabel,
            SaveTooltip: SaveTooltip,
            SaveEnabled: sessionOpen,
            SaveUssClass: ActionUssClass("save", sessionOpen, blocked: false),
            SaveStatusText: SaveStatus(sessionOpen, isDirty, lastSave),
            ExportLabel: ExportLabel,
            ExportTooltip: ExportTooltip,
            ExportEnabled: exportEnabled,
            ExportBlocked: exportBlocked,
            ExportUssClass: ActionUssClass("export", exportEnabled, exportBlocked),
            ExportStatusText: ExportStatus(sessionOpen, isDirty, blocking, lastExport),
            BlockingFindingCount: blocking,
            LastExportArtifactPath: lastExport?.Allowed == true ? lastExport.ArtifactPath : null);
    }

    /// <summary>Derives Save/Export chrome from the existing shell state (error findings gate Export).</summary>
    public static EditorSaveExportState FromShell(
        ScenarioEditorShellState shell,
        ScenarioDraftSaveOutcome? lastSave = null,
        ScenarioValidatedExportOutcome? lastExport = null) =>
        Bind(shell.SessionOpen, shell.IsDirty, shell.ErrorFindingCount, lastSave, lastExport);

    private static string SaveStatus(bool sessionOpen, bool isDirty, ScenarioDraftSaveOutcome? lastSave)
    {
        if (!sessionOpen)
        {
            return "No scenario open.";
        }

        if (isDirty)
        {
            return "Unsaved draft changes — Save draft keeps work in progress (validation not required).";
        }

        return lastSave?.StatusText ?? "Draft saved — not exported.";
    }

    private static string ExportStatus(bool sessionOpen, bool isDirty, int blocking, ScenarioValidatedExportOutcome? lastExport)
    {
        if (!sessionOpen)
        {
            return "Open a scenario to export.";
        }

        if (blocking > 0)
        {
            var noun = blocking == 1 ? "blocking finding" : "blocking findings";
            return $"Export blocked — {blocking} {noun} must be resolved. Save draft is still available.";
        }

        if (lastExport is { Allowed: true })
        {
            return isDirty
                ? $"Draft changed since last export ({lastExport.ArtifactPath}) — export again to refresh the artifact."
                : $"Exported validated artifact → {lastExport.ArtifactPath}";
        }

        return "Export ready — writes a validated artifact separate from the draft.";
    }

    private static string ActionUssClass(string verb, bool enabled, bool blocked)
    {
        var cls = $"editor-persist-action editor-persist-action--{verb}";
        if (blocked)
        {
            cls += " editor-persist-action--blocked";
        }

        if (!enabled)
        {
            cls += " editor-persist-action--disabled";
        }

        return cls;
    }
}
