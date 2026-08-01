namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Scenario.Campaign;

/// <summary>
/// Headless apply path for campaign library presentation (CMD-27.12).
/// Unity hosts bind <see cref="CampaignLibraryPresentation.Lines"/> and preview fields without re-formatting.
/// Campaigns are listed as a separate artifact class from flat scenarios.
/// </summary>
public static class CampaignLibraryApplyState
{
    /// <summary>CMD-27.12 zero-state instruction when nothing is selected.</summary>
    public const string ZeroStateInstruction = "Select a campaign";

    public static CampaignLibraryPresentation Apply(IReadOnlyList<CampaignLibraryEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return CampaignLibraryPresentation.Empty;
        }

        var rows = new List<CampaignLibraryDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatRowLine(entry);
            lines.Add(line);
            rows.Add(new CampaignLibraryDisplayRow(
                entry.CampaignId,
                entry.Title,
                line,
                entry.MemberCount,
                entry.CompletedCount,
                entry.NextScenarioId,
                entry.Available,
                entry.UnavailableReason,
                entry.SourcePath));
        }

        return new CampaignLibraryPresentation(rows, lines, entries.Count);
    }

    /// <summary>
    /// List-row label: title + progress + availability; unavailable rows state their reason on the row.
    /// </summary>
    public static string FormatRowLine(CampaignLibraryEntry entry)
    {
        var progress = $"{entry.CompletedCount}/{entry.MemberCount}";
        if (entry.Available)
        {
            return $"{entry.Title}  [{progress}]  [available]";
        }

        var reason = string.IsNullOrWhiteSpace(entry.UnavailableReason)
            ? "UNAVAILABLE"
            : entry.UnavailableReason;
        return $"{entry.Title}  [{progress}]  [unavailable: {reason}]";
    }

    /// <summary>Preview pane fields for the selected campaign, or zero-state when null.</summary>
    public static CampaignLibraryPreviewPresentation ApplyPreview(CampaignLibraryEntry? selected)
    {
        if (selected is null)
        {
            return CampaignLibraryPreviewPresentation.ZeroState;
        }

        var availability = selected.Available
            ? "Available"
            : $"Unavailable ({selected.UnavailableReason ?? "—"})";

        var next = string.IsNullOrWhiteSpace(selected.NextScenarioId)
            ? "Next: —"
            : $"Next: {selected.NextScenarioId}";

        return new CampaignLibraryPreviewPresentation(
            IsZeroState: false,
            InstructionLine: string.Empty,
            TitleLine: selected.Title,
            CampaignIdLine: $"ID: {selected.CampaignId}",
            AvailabilityLine: availability,
            ProgressLine: $"Progress: {selected.CompletedCount}/{selected.MemberCount}",
            NextScenarioLine: next,
            SourcePathLine: selected.SourcePath);
    }
}

/// <summary>One bound list row for the campaign library ListView.</summary>
public sealed record CampaignLibraryDisplayRow(
    string CampaignId,
    string Title,
    string DisplayLine,
    int MemberCount,
    int CompletedCount,
    string? NextScenarioId,
    bool Available,
    string? UnavailableReason,
    string SourcePath);

/// <summary>Presentation bundle for hosts / tests (campaign list side).</summary>
public sealed record CampaignLibraryPresentation(
    IReadOnlyList<CampaignLibraryDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    int Count)
{
    public static CampaignLibraryPresentation Empty { get; } =
        new(Array.Empty<CampaignLibraryDisplayRow>(), Array.Empty<string>(), 0);
}

/// <summary>Campaign preview pane presentation (CMD-27.12 zero-state: "Select a campaign").</summary>
public sealed record CampaignLibraryPreviewPresentation(
    bool IsZeroState,
    string InstructionLine,
    string TitleLine,
    string CampaignIdLine,
    string AvailabilityLine,
    string ProgressLine,
    string NextScenarioLine,
    string SourcePathLine)
{
    public static CampaignLibraryPreviewPresentation ZeroState { get; } = new(
        IsZeroState: true,
        InstructionLine: CampaignLibraryApplyState.ZeroStateInstruction,
        TitleLine: string.Empty,
        CampaignIdLine: string.Empty,
        AvailabilityLine: string.Empty,
        ProgressLine: string.Empty,
        NextScenarioLine: string.Empty,
        SourcePathLine: string.Empty);
}
