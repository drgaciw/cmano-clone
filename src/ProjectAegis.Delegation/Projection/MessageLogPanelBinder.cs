namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps projected <see cref="MessageLogLine"/> list to HUD message log panel rows.</summary>
public static class MessageLogPanelBinder
{
    private const string PlayerOrderCategory = "PLAYER_ORDER";

    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines) =>
        Bind(lines, lifecycleStates: null);

    /// <summary>
    /// T2 overload (req 20 §Order lifecycle): when <paramref name="lifecycleStates"/> is supplied
    /// (typically <c>OrderLifecycleProjection.Project(log)</c>), <c>PLAYER_ORDER</c> rows carry their
    /// resolved <see cref="OrderLifecycleState"/> for row-level chip styling. Other categories are
    /// unaffected. Passing <c>null</c> is identical to the single-argument overload.
    /// </summary>
    public static MessageLogPanelState Bind(
        IReadOnlyList<MessageLogLine> lines,
        IReadOnlyDictionary<OrderLifecycleProjection.OrderKey, OrderLifecycleState>? lifecycleStates)
    {
        var rows = new List<MessageLogDisplayRow>(lines.Count);
        foreach (var line in lines)
        {
            rows.Add(new MessageLogDisplayRow(
                line.Category,
                FormatLine(line.Category, line.Text),
                ResolveLifecycleState(line, lifecycleStates)));
        }

        return new MessageLogPanelState(rows);
    }

    private static OrderLifecycleState? ResolveLifecycleState(
        MessageLogLine line,
        IReadOnlyDictionary<OrderLifecycleProjection.OrderKey, OrderLifecycleState>? lifecycleStates)
    {
        if (lifecycleStates == null ||
            line.UnitId == null ||
            !string.Equals(line.Category, PlayerOrderCategory, StringComparison.Ordinal))
        {
            return null;
        }

        var key = new OrderLifecycleProjection.OrderKey(line.UnitId, line.SequenceId);
        return lifecycleStates.TryGetValue(key, out var state) ? state : null;
    }

    private static string FormatLine(string category, string text) => $"[{category}] {text}";
}