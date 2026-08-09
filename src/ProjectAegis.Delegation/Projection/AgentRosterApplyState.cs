namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for agent roster presentation (CMD-37 + S109-02).
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
                entry.ModeLabel,
                entry.AttentionLabel,
                AccessibleAttentionLabel: entry.AttentionLabel));
        }

        return new AgentRosterPresentation(rows, lines, entries.Count);
    }

    /// <summary>
    /// Apply roster entries enriched with full attention rows (S109-02).
    /// Attention labels come from the decision-time projection contract.
    /// </summary>
    public static AgentRosterPresentation ApplyWithAttention(
        IReadOnlyList<AgentRosterEntry>? entries,
        IReadOnlyDictionary<string, AgentAttentionRow>? attentionByAgentId)
    {
        if (entries is null || entries.Count == 0)
        {
            return AgentRosterPresentation.Empty;
        }

        attentionByAgentId ??= new Dictionary<string, AgentAttentionRow>(StringComparer.Ordinal);
        var rows = new List<AgentRosterDisplayRow>(entries.Count);
        var lines = new List<string>(entries.Count);
        foreach (var entry in entries)
        {
            AgentAttentionRow? att = null;
            if (!string.IsNullOrEmpty(entry.AgentId))
            {
                attentionByAgentId.TryGetValue(entry.AgentId, out att);
            }

            var attentionLabel = att is null
                ? entry.AttentionLabel
                : $"{att.LoadBadge} · {att.TierLabel}";
            var accessible = att?.AccessibleLabel ?? entry.AttentionLabel;
            var line = FormatLine(entry with { AttentionLabel = attentionLabel });
            lines.Add(line);
            rows.Add(new AgentRosterDisplayRow(
                entry.UnitId,
                entry.AgentId,
                line,
                entry.StatusLabel,
                entry.ModeLabel,
                attentionLabel,
                accessible));
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
    string ModeLabel,
    string AttentionLabel = "ATTENTION: —",
    string AccessibleAttentionLabel = "Attention sample unavailable");

/// <summary>Presentation bundle for hosts / tests.</summary>
public sealed record AgentRosterPresentation(
    IReadOnlyList<AgentRosterDisplayRow> Rows,
    IReadOnlyList<string> Lines,
    int Count)
{
    public static AgentRosterPresentation Empty { get; } =
        new(Array.Empty<AgentRosterDisplayRow>(), Array.Empty<string>(), 0);
}
