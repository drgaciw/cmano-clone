namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps projected <see cref="MessageLogLine"/> list to HUD message log panel rows.</summary>
public static class MessageLogPanelBinder
{
    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines)
    {
        var rows = new List<MessageLogDisplayRow>(lines.Count);
        foreach (var line in lines)
        {
            rows.Add(new MessageLogDisplayRow(
                line.Category,
                FormatLine(line.Category, line.Text),
                line.SequenceId,
                line.UnitId));
        }

        return new MessageLogPanelState(rows);
    }

    private static string FormatLine(string category, string text) => $"[{category}] {text}";
}