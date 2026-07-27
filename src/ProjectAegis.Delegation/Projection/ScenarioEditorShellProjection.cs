using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAegis.Delegation.Projection;

/// <summary>SE-UX P2.1/P2.2: primary workspace mode in the Scenario Editor shell.</summary>
public enum ScenarioEditorShellMode
{
    /// <summary>Map-first ORBAT / geometry authoring (P2.1).</summary>
    Map,

    /// <summary>Mission Board list / filter / clone / template (P2.2).</summary>
    MissionBoard,
}

/// <summary>Live Findings dock severity filter (P-SE-02).</summary>
public enum ScenarioEditorFindingsFilter
{
    All,
    Errors,
    Warnings,
}

/// <summary>Severity for findings dock rows (text tag + color, not color-only).</summary>
public enum ScenarioEditorFindingSeverity
{
    Error,
    Warning,
    Info,
}

/// <summary>One bindable Live Findings dock row.</summary>
public sealed record ScenarioEditorFindingRow(
    string SeverityTag,
    ScenarioEditorFindingSeverity Severity,
    string Code,
    string Message,
    string? EntityId,
    bool PreferMissionBoardMode);

/// <summary>Bindable Scenario Editor shell chrome — hosts remain thin binders.</summary>
public sealed record ScenarioEditorShellState(
    ScenarioEditorShellMode Mode,
    string ModeTitle,
    string RootUssClass,
    bool MapTabActive,
    bool MissionBoardTabActive,
    bool MapContentVisible,
    bool MissionBoardContentVisible,
    bool EventsTabEnabled,
    string? SelectedEntityId,
    string StatusSummary,
    int ErrorFindingCount,
    int WarningFindingCount,
    bool PlayBlocked,
    string PlayBlockReason,
    string ScenarioTitle,
    bool IsDirty,
    int EditVersion,
    string DirtyLabel,
    bool SessionOpen,
    bool SaveEnabled,
    bool LoadEnabled,
    bool UndoEnabled,
    bool RedoEnabled,
    bool PlayEnabled,
    bool SampleEnabled,
    ScenarioEditorFindingsFilter FindingsFilter,
    IReadOnlyList<ScenarioEditorFindingRow> FindingRows,
    IReadOnlyList<ScenarioEditorFindingRow> VisibleFindingRows,
    string FindingsSummaryText,
    string SelectionInspectorText);

/// <summary>
/// Pure projection for Map | Mission Board shell navigation and product chrome
/// (top bar, Play/Sample gate, findings dock). Events tab is reserved (P2.3).
/// Play/Sample gating uses error findings only. No write-gate / bus / sim.
/// </summary>
public static class ScenarioEditorShellProjection
{
    private static readonly IReadOnlyList<ScenarioEditorFindingRow> EmptyFindings =
        Array.Empty<ScenarioEditorFindingRow>();

    public static ScenarioEditorShellState Bind(
        ScenarioEditorShellMode mode = ScenarioEditorShellMode.Map,
        string? selectedEntityId = null,
        string statusSummary = "",
        int errorFindingCount = 0,
        int warningFindingCount = 0,
        string scenarioTitle = "Untitled Scenario",
        bool isDirty = false,
        int editVersion = 0,
        bool sessionOpen = false,
        bool undoEnabled = false,
        bool redoEnabled = false,
        ScenarioEditorFindingsFilter findingsFilter = ScenarioEditorFindingsFilter.All,
        IReadOnlyList<ScenarioEditorFindingRow>? findingRows = null)
    {
        var isMap = mode == ScenarioEditorShellMode.Map;
        var playBlocked = errorFindingCount > 0;
        var rows = findingRows ?? EmptyFindings;
        var visible = ScenarioEditorFindingsProjection.Filter(rows, findingsFilter);
        var saveEnabled = sessionOpen;
        var playEnabled = sessionOpen && !playBlocked;
        return new ScenarioEditorShellState(
            Mode: mode,
            ModeTitle: isMap ? "MAP AUTHORING" : "MISSION BOARD",
            RootUssClass: isMap ? "scenario-editor-shell--map" : "scenario-editor-shell--mission-board",
            MapTabActive: isMap,
            MissionBoardTabActive: !isMap,
            MapContentVisible: isMap,
            MissionBoardContentVisible: !isMap,
            EventsTabEnabled: false,
            SelectedEntityId: selectedEntityId,
            StatusSummary: statusSummary ?? string.Empty,
            ErrorFindingCount: errorFindingCount,
            WarningFindingCount: warningFindingCount,
            PlayBlocked: playBlocked,
            PlayBlockReason: playBlocked
                ? $"Blocked by {errorFindingCount} error finding{(errorFindingCount == 1 ? string.Empty : "s")}"
                : string.Empty,
            ScenarioTitle: string.IsNullOrWhiteSpace(scenarioTitle) ? "Untitled Scenario" : scenarioTitle,
            IsDirty: isDirty,
            EditVersion: editVersion,
            DirtyLabel: isDirty ? "• unsaved" : "saved",
            SessionOpen: sessionOpen,
            SaveEnabled: saveEnabled,
            LoadEnabled: true,
            UndoEnabled: sessionOpen && undoEnabled,
            RedoEnabled: sessionOpen && redoEnabled,
            PlayEnabled: playEnabled,
            SampleEnabled: playEnabled,
            FindingsFilter: findingsFilter,
            FindingRows: rows,
            VisibleFindingRows: visible,
            FindingsSummaryText: ScenarioEditorFindingsProjection.FormatSummary(
                errorFindingCount,
                warningFindingCount),
            SelectionInspectorText: string.IsNullOrEmpty(selectedEntityId)
                ? "No selection"
                : $"Selected: {selectedEntityId}");
    }

    public static ScenarioEditorShellState WithMode(
        ScenarioEditorShellState state,
        ScenarioEditorShellMode mode) =>
        Rebind(state, mode: mode);

    public static ScenarioEditorShellState WithSelection(
        ScenarioEditorShellState state,
        string? selectedEntityId) =>
        Rebind(state, selectedEntityId: selectedEntityId);

    public static ScenarioEditorShellState WithStatus(
        ScenarioEditorShellState state,
        string statusSummary) =>
        Rebind(state, statusSummary: statusSummary);

    public static ScenarioEditorShellState WithFindings(
        ScenarioEditorShellState state,
        int errorCount,
        int warningCount) =>
        Rebind(state, errorFindingCount: errorCount, warningFindingCount: warningCount);

    public static ScenarioEditorShellState WithTopBar(
        ScenarioEditorShellState state,
        string scenarioTitle,
        bool isDirty,
        int editVersion,
        bool sessionOpen,
        bool undoEnabled = false,
        bool redoEnabled = false) =>
        Rebind(
            state,
            scenarioTitle: scenarioTitle,
            isDirty: isDirty,
            editVersion: editVersion,
            sessionOpen: sessionOpen,
            undoEnabled: undoEnabled,
            redoEnabled: redoEnabled);

    public static ScenarioEditorShellState WithFindingRows(
        ScenarioEditorShellState state,
        IReadOnlyList<ScenarioEditorFindingRow> rows,
        ScenarioEditorFindingsFilter? filter = null)
    {
        rows ??= EmptyFindings;
        var errorCount = rows.Count(r => r.Severity == ScenarioEditorFindingSeverity.Error);
        var warningCount = rows.Count(r => r.Severity == ScenarioEditorFindingSeverity.Warning);
        return Rebind(
            state,
            errorFindingCount: errorCount,
            warningFindingCount: warningCount,
            findingsFilter: filter ?? state.FindingsFilter,
            findingRows: rows);
    }

    public static ScenarioEditorShellState WithFindingsFilter(
        ScenarioEditorShellState state,
        ScenarioEditorFindingsFilter filter) =>
        Rebind(state, findingsFilter: filter, findingRows: state.FindingRows);

    public static ScenarioEditorShellState JumpToFinding(
        ScenarioEditorShellState state,
        ScenarioEditorFindingRow row)
    {
        var mode = row.PreferMissionBoardMode
            ? ScenarioEditorShellMode.MissionBoard
            : state.Mode;
        return Rebind(state, mode: mode, selectedEntityId: row.EntityId);
    }

    public static ScenarioEditorShellState CycleMode(ScenarioEditorShellState state) =>
        WithMode(
            state,
            state.Mode == ScenarioEditorShellMode.Map
                ? ScenarioEditorShellMode.MissionBoard
                : ScenarioEditorShellMode.Map);

    public static string TabUssClass(bool isActive, bool isEnabled = true)
    {
        if (!isEnabled)
        {
            return "scenario-editor-shell-tab scenario-editor-shell-tab--disabled";
        }

        return isActive
            ? "scenario-editor-shell-tab scenario-editor-shell-tab--active"
            : "scenario-editor-shell-tab";
    }

    public static string ActionEnabledUssClass(bool enabled) =>
        enabled
            ? "scenario-editor-shell-action"
            : "scenario-editor-shell-action scenario-editor-shell-action--disabled";

    private static ScenarioEditorShellState Rebind(
        ScenarioEditorShellState state,
        ScenarioEditorShellMode? mode = null,
        string? selectedEntityId = null,
        bool clearSelection = false,
        string? statusSummary = null,
        int? errorFindingCount = null,
        int? warningFindingCount = null,
        string? scenarioTitle = null,
        bool? isDirty = null,
        int? editVersion = null,
        bool? sessionOpen = null,
        bool? undoEnabled = null,
        bool? redoEnabled = null,
        ScenarioEditorFindingsFilter? findingsFilter = null,
        IReadOnlyList<ScenarioEditorFindingRow>? findingRows = null) =>
        Bind(
            mode: mode ?? state.Mode,
            selectedEntityId: clearSelection ? null : (selectedEntityId ?? state.SelectedEntityId),
            statusSummary: statusSummary ?? state.StatusSummary,
            errorFindingCount: errorFindingCount ?? state.ErrorFindingCount,
            warningFindingCount: warningFindingCount ?? state.WarningFindingCount,
            scenarioTitle: scenarioTitle ?? state.ScenarioTitle,
            isDirty: isDirty ?? state.IsDirty,
            editVersion: editVersion ?? state.EditVersion,
            sessionOpen: sessionOpen ?? state.SessionOpen,
            undoEnabled: undoEnabled ?? state.UndoEnabled,
            redoEnabled: redoEnabled ?? state.RedoEnabled,
            findingsFilter: findingsFilter ?? state.FindingsFilter,
            findingRows: findingRows ?? state.FindingRows);
}

/// <summary>Pure helpers for Live Findings dock rows and filters (P-SE-02).</summary>
public static class ScenarioEditorFindingsProjection
{
    public static ScenarioEditorFindingRow CreateRow(
        ScenarioEditorFindingSeverity severity,
        string code,
        string message,
        string? entityId,
        bool preferMissionBoard) =>
        new(
            SeverityTag: SeverityTag(severity),
            Severity: severity,
            Code: code ?? string.Empty,
            Message: message ?? string.Empty,
            EntityId: entityId,
            PreferMissionBoardMode: preferMissionBoard);

    public static string SeverityTag(ScenarioEditorFindingSeverity severity) =>
        severity switch
        {
            ScenarioEditorFindingSeverity.Error => "ERROR",
            ScenarioEditorFindingSeverity.Warning => "WARN",
            _ => "INFO",
        };

    public static string FormatSummary(int errorCount, int warningCount) =>
        $"Findings: {errorCount} error{(errorCount == 1 ? string.Empty : "s")} · {warningCount} warning{(warningCount == 1 ? string.Empty : "s")}";

    public static string FormatRowLabel(ScenarioEditorFindingRow row) =>
        $"[{row.SeverityTag}] {row.Code} — {row.Message}";

    public static IReadOnlyList<ScenarioEditorFindingRow> Filter(
        IReadOnlyList<ScenarioEditorFindingRow> rows,
        ScenarioEditorFindingsFilter filter)
    {
        if (rows == null || rows.Count == 0)
        {
            return Array.Empty<ScenarioEditorFindingRow>();
        }

        return filter switch
        {
            ScenarioEditorFindingsFilter.Errors => rows
                .Where(r => r.Severity == ScenarioEditorFindingSeverity.Error)
                .ToArray(),
            ScenarioEditorFindingsFilter.Warnings => rows
                .Where(r => r.Severity == ScenarioEditorFindingSeverity.Warning)
                .ToArray(),
            _ => rows,
        };
    }

    /// <summary>
    /// Heuristic: mission/strike/patrol/ferry/support codes prefer Mission Board on jump.
    /// Hosts may override via <see cref="ScenarioEditorFindingRow.PreferMissionBoardMode"/>.
    /// </summary>
    public static bool PreferMissionBoardForCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var c = code.ToUpperInvariant();
        return c.Contains("MISSION", StringComparison.Ordinal)
               || c.Contains("STRIKE", StringComparison.Ordinal)
               || c.Contains("PATROL", StringComparison.Ordinal)
               || c.Contains("FERRY", StringComparison.Ordinal)
               || c.Contains("SUPPORT", StringComparison.Ordinal)
               || c.Contains("TARGET", StringComparison.Ordinal);
    }

    public static string FilterChipUssClass(
        ScenarioEditorFindingsFilter chip,
        ScenarioEditorFindingsFilter active) =>
        chip == active
            ? "scenario-editor-shell-findings-filter scenario-editor-shell-findings-filter--active"
            : "scenario-editor-shell-findings-filter";

    public static string RowSeverityUssClass(ScenarioEditorFindingSeverity severity) =>
        severity switch
        {
            ScenarioEditorFindingSeverity.Error =>
                "scenario-editor-shell-finding-row scenario-editor-shell-finding-row--error",
            ScenarioEditorFindingSeverity.Warning =>
                "scenario-editor-shell-finding-row scenario-editor-shell-finding-row--warning",
            _ => "scenario-editor-shell-finding-row scenario-editor-shell-finding-row--info",
        };
}
