namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for agent roster presentation (CMD-37).
/// Unity hosts bind <see cref="AgentRosterPresentation.Lines"/> onto Labels without re-formatting.
/// </summary>
public static class AgentRosterApplyState
{
    public static AgentRosterPresentation Apply(IReadOnlyList<AgentRosterEntry>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return AgentRosterPresentation.Empty;
        }

        var rows = new List<AgentRosterDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            var line = FormatLine(entry);
            lines.Add(line);
            rows.Add(new AgentRosterDisplayRow(
                entry.UnitId,
                entry.AgentId,
                line,
                entry.StatusLabel,
                entry.ModeLabel));
        }

        return new AgentRosterPresentation(rows, lines, entries.Count);
    }

    public static string FormatLine(AgentRosterEntry entry)
    {
        var agent = string.IsNullOrWhiteSpace(entry.AgentId) ? "—" : entry.AgentId;
        return $"{entry.UnitId}  agent={agent}  [{entry.StatusLabel}]  {entry.AutonomyLabel}  mode={entry.ModeLabel}  {entry.AttentionLabel}";
    }
}

/// <summary>One bound list row for the agent roster ListView.</summary>
public sealed record AgentRosterDisplayRow(
    string UnitId,
    string AgentId,
    string DisplayLine,
    string StatusLabel,
    string ModeLabel);

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record AgentRosterPresentation(
    IReadOnlyList<AgentRosterDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    int Count)
{
    public static AgentRosterPresentation Empty { get; } =
        new(Array.Empty<AgentRosterDisplayRow>(), Array.Empty<string>(), 0);
}
