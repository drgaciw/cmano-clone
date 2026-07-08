namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// A <see cref="MessageLogLine"/> annotated with its <see cref="AlertSeverity"/> tier (req 20 §Alerting
/// and Interruption, TR-c2-007). Additive — carries the same fields as <see cref="MessageLogLine"/> plus
/// severity, so it never duplicates or replaces <see cref="MessageLogProjection"/> output.
/// </summary>
public sealed record AlertItem(
    ulong SequenceId,
    double SimTime,
    string Category,
    string Text,
    string? UnitId,
    AlertSeverity Severity);

/// <summary>
/// Severity/alert consumer over <see cref="MessageLogLine"/> (Track T3, req 20 §Alerting and
/// Interruption, TR-c2-007/008). Purely additive: reads the same lines <see cref="MessageLogProjection"/>
/// produces and <see cref="MessageLogPanelBinder"/> renders, and tags each with the tier from
/// <see cref="AlertSeverityMap"/> — the single source of truth for category → severity. Does not alter
/// or wrap <see cref="MessageLogProjection"/>'s own output.
/// </summary>
public static class AlertProjection
{
    /// <summary>Projects every line to an <see cref="AlertItem"/>, preserving input order.</summary>
    public static IReadOnlyList<AlertItem> Project(IReadOnlyList<MessageLogLine> lines)
    {
        var items = new List<AlertItem>(lines.Count);
        foreach (var line in lines)
        {
            items.Add(ToAlertItem(line));
        }

        return items;
    }

    /// <summary>Projects only lines whose category maps to <paramref name="severity"/>, preserving order.</summary>
    public static IReadOnlyList<AlertItem> ProjectSeverity(
        IReadOnlyList<MessageLogLine> lines,
        AlertSeverity severity)
    {
        var items = new List<AlertItem>();
        foreach (var line in lines)
        {
            var item = ToAlertItem(line);
            if (item.Severity == severity)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static AlertItem ToAlertItem(MessageLogLine line) => new(
        line.SequenceId,
        line.SimTime,
        line.Category,
        line.Text,
        line.UnitId,
        AlertSeverityMap.ForCategory(line.Category));
}
