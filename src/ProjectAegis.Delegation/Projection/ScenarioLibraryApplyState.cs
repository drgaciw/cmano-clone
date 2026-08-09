namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Scenario;

/// <summary>
/// Headless apply path for scenario library presentation (CMD-27).
/// Unity hosts bind <see cref="ScenarioLibraryPresentation.Lines"/> and preview fields without re-formatting.
/// </summary>
public static class ScenarioLibraryApplyState
{
    /// <summary>CMD-27.6 zero-state instruction when nothing is selected.</summary>
    public const string ZeroStateInstruction =
        "Select a scenario to preview title, determinism metadata, and pre-load feasibility.";

    public static ScenarioLibraryPresentation Apply(IReadOnlyList<ScenarioLibraryEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return ScenarioLibraryPresentation.Empty;
        }

        var rows = new List<ScenarioLibraryDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatRowLine(entry);
            lines.Add(line);
            rows.Add(new ScenarioLibraryDisplayRow(
                entry.ScenarioId,
                entry.Title,
                line,
                entry.Available,
                entry.UnavailableReason,
                entry.SourcePath));
        }

        return new ScenarioLibraryPresentation(rows, lines, entries.Count);
    }

    /// <summary>
    /// List-row label: title + availability; unavailable rows state their reason on the row (CMD-27.7).
    /// </summary>
    public static string FormatRowLine(ScenarioLibraryEntry entry)
    {
        if (entry.Available)
        {
            return $"{entry.Title}  [available]";
        }

        var reason = string.IsNullOrWhiteSpace(entry.UnavailableReason)
            ? "UNAVAILABLE"
            : entry.UnavailableReason;
        return $"{entry.Title}  [unavailable: {reason}]";
    }

    /// <summary>Preview pane fields for the selected entry, or zero-state when null (CMD-27.6).</summary>
    public static ScenarioLibraryPreviewPresentation ApplyPreview(ScenarioLibraryEntry? selected)
    {
        if (selected is null)
        {
            return ScenarioLibraryPreviewPresentation.ZeroState;
        }

        var availability = selected.Available
            ? "Available"
            : $"Unavailable ({selected.UnavailableReason ?? "—"})";

        return new ScenarioLibraryPreviewPresentation(
            IsZeroState: false,
            InstructionLine: string.Empty,
            TitleLine: selected.Title,
            ScenarioIdLine: $"ID: {selected.ScenarioId}",
            AvailabilityLine: availability,
            PolicyLine: $"Policy: {selected.PolicyId}",
            TlBranchLine: $"TL: {selected.TlBranch}",
            SeedLine: $"Seed: {selected.Seed}",
            LocationYearLine: $"{selected.Location} — {selected.Year}",
            ProvenanceLine: $"Provenance: {selected.ProvenanceLabel}",
            DifficultyLine: $"Difficulty: {selected.Difficulty}",
            ComplexityLine: $"Complexity: {selected.Complexity}",
            SourcePathLine: selected.SourcePath);
    }
}

/// <summary>One bound list row for the scenario library ListView.</summary>
public sealed record ScenarioLibraryDisplayRow(
    string ScenarioId,
    string Title,
    string DisplayLine,
    bool Available,
    string? UnavailableReason,
    string SourcePath);

/// <summary>Presentation bundle for hosts / tests (list side).</summary>
public sealed record ScenarioLibraryPresentation(
    IReadOnlyList<ScenarioLibraryDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    int Count)
{
    public static ScenarioLibraryPresentation Empty { get; } =
        new(Array.Empty<ScenarioLibraryDisplayRow>(), Array.Empty<string>(), 0);
}

/// <summary>Preview pane presentation (CMD-27.6).</summary>
public sealed record ScenarioLibraryPreviewPresentation(
    bool IsZeroState,
    string InstructionLine,
    string TitleLine,
    string ScenarioIdLine,
    string AvailabilityLine,
    string PolicyLine,
    string TlBranchLine,
    string SeedLine,
    string LocationYearLine,
    string ProvenanceLine,
    string DifficultyLine,
    string ComplexityLine,
    string SourcePathLine)
{
    public static ScenarioLibraryPreviewPresentation ZeroState { get; } = new(
        IsZeroState: true,
        InstructionLine: ScenarioLibraryApplyState.ZeroStateInstruction,
        TitleLine: string.Empty,
        ScenarioIdLine: string.Empty,
        AvailabilityLine: string.Empty,
        PolicyLine: string.Empty,
        TlBranchLine: string.Empty,
        SeedLine: string.Empty,
        LocationYearLine: string.Empty,
        ProvenanceLine: string.Empty,
        DifficultyLine: string.Empty,
        ComplexityLine: string.Empty,
        SourcePathLine: string.Empty);
}
