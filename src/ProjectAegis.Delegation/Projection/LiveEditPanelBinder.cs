namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Headless binder for the live-edit findings panel (CMD-35).
/// Produces display rows (finding codes + severity tags) hosts list without re-formatting.
/// </summary>
public static class LiveEditPanelBinder
{
    /// <summary>Bind an already-applied presentation into list-display state.</summary>
    public static LiveEditPanelState Bind(LiveEditSessionPresentation? presentation)
    {
        if (presentation is null)
        {
            return LiveEditPanelState.Empty;
        }

        var rows = new List<LiveEditFindingDisplayRow>(presentation.Findings.Count);
        foreach (var f in presentation.Findings)
        {
            if (f is null)
            {
                continue;
            }

            rows.Add(new LiveEditFindingDisplayRow(
                Code: f.Code ?? string.Empty,
                Severity: f.Severity ?? "Info",
                DisplayLine: FormatDisplayLine(f),
                Path: f.Path));
        }

        return new LiveEditPanelState(
            StatusLine: presentation.StatusLine ?? string.Empty,
            EditVersion: presentation.EditVersion,
            BlockingCount: presentation.BlockingCount,
            WarningCount: presentation.WarningCount,
            Rows: rows,
            FindingCodes: rows.Select(r => r.Code).ToArray(),
            CommitEnabled: presentation.CommitEnabled,
            CommitDisabledReason: presentation.CommitDisabledReason,
            LastMutationOk: presentation.LastMutationOk,
            LastMutationReason: presentation.LastMutationReason);
    }

    /// <summary>Apply-then-bind from a validation report and mutation outcome.</summary>
    public static LiveEditPanelState BindFromReport(
        ValidationReport? report,
        int editVersion,
        bool lastMutationOk,
        string? lastReason) =>
        Bind(LiveEditApplyState.FromValidationReport(report, editVersion, lastMutationOk, lastReason));

    /// <summary>Apply-then-bind from a bus mutation result (preferred host path).</summary>
    public static LiveEditPanelState BindFromMutation(ScenarioMutationResult result) =>
        Bind(LiveEditApplyState.FromMutationResult(result));

    public static string FormatDisplayLine(LiveEditFindingEntry entry)
    {
        var code = entry.Code ?? string.Empty;
        var severity = entry.Severity ?? "Info";
        var message = entry.Message ?? string.Empty;
        if (string.IsNullOrEmpty(entry.Path))
        {
            return $"[{severity}] {code} — {message}";
        }

        return $"[{severity}] {code} @ {entry.Path} — {message}";
    }
}

/// <summary>One scroll-list row for the live-edit findings panel.</summary>
public sealed record LiveEditFindingDisplayRow(
    string Code,
    string Severity,
    string DisplayLine,
    string? Path);

/// <summary>Bound panel state for <c>LiveEditPanelHost</c> (status + codes list + commit gate).</summary>
public sealed record LiveEditPanelState(
    string StatusLine,
    int EditVersion,
    int BlockingCount,
    int WarningCount,
    IReadOnlyList<LiveEditFindingDisplayRow> Rows,
    IReadOnlyList<string> FindingCodes,
    bool CommitEnabled,
    string? CommitDisabledReason,
    bool LastMutationOk,
    string? LastMutationReason)
{
    public static LiveEditPanelState Empty { get; } = new(
        StatusLine: LiveEditSessionPresentation.Empty.StatusLine,
        EditVersion: 0,
        BlockingCount: 0,
        WarningCount: 0,
        Rows: Array.Empty<LiveEditFindingDisplayRow>(),
        FindingCodes: Array.Empty<string>(),
        CommitEnabled: true,
        CommitDisabledReason: null,
        LastMutationOk: true,
        LastMutationReason: null);
}
