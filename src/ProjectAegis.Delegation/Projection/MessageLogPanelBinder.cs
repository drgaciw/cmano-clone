namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Maps projected <see cref="MessageLogLine"/> list to HUD message log panel rows.
/// The live HUD binds lines from <c>GetLiveOrderLogView()</c> with
/// <see cref="MessageLogBindingSource.LiveOrderLogView"/>; replay / AAR binds the full
/// <c>DecisionLog</c> with <see cref="MessageLogBindingSource.FullDecisionLog"/>.
/// </summary>
public static class MessageLogPanelBinder
{
    /// <summary>Bind lines projected from the full <c>DecisionLog</c> (replay / AAR).</summary>
    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines) =>
        Bind(lines, MessageLogBindingSource.FullDecisionLog);

    /// <summary>Bind lines and record which order-log view they came from.</summary>
    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines, MessageLogBindingSource source)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        var rows = new List<MessageLogDisplayRow>(lines.Count);
        foreach (var line in lines)
        {
            rows.Add(new MessageLogDisplayRow(
                line.Category,
                FormatLine(line.Category, line.Text),
                line.SequenceId,
                line.UnitId,
                MessageLogCategoryClassMap.CssClassFor(line.Category)));
        }

        return new MessageLogPanelState(rows, source);
    }

    private static string FormatLine(string category, string text) => $"[{category}] {text}";
}
