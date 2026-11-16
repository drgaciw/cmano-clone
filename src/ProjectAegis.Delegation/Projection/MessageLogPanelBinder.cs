namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Maps projected <see cref="MessageLogLine"/> list to HUD message log panel rows.
/// The live HUD binds lines from <c>GetLiveOrderLogView()</c> with
/// <see cref="MessageLogBindingSource.LiveOrderLogView"/>; replay / AAR binds the full
/// <c>DecisionLog</c> with <see cref="MessageLogBindingSource.FullDecisionLog"/>.
/// DRG-412 adds categorized vs raw view modes and a read-only export path.
/// </summary>
public static class MessageLogPanelBinder
{
    /// <summary>Bind lines projected from the full <c>DecisionLog</c> (replay / AAR), categorized.</summary>
    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines) =>
        Bind(lines, MessageLogBindingSource.FullDecisionLog, MessageLogPanelViewMode.Categorized);

    /// <summary>Bind lines and record which order-log view they came from (categorized).</summary>
    public static MessageLogPanelState Bind(IReadOnlyList<MessageLogLine> lines, MessageLogBindingSource source) =>
        Bind(lines, source, MessageLogPanelViewMode.Categorized);

    /// <summary>Bind lines with order-log source and categorized vs raw presentation.</summary>
    public static MessageLogPanelState Bind(
        IReadOnlyList<MessageLogLine> lines,
        MessageLogBindingSource source,
        MessageLogPanelViewMode viewMode)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        var rows = new List<MessageLogDisplayRow>(lines.Count);
        foreach (var line in lines)
        {
            var display = viewMode == MessageLogPanelViewMode.Raw
                ? MessageLogTextExport.FormatLine(line)
                : FormatCategorized(line.Category, line.Text);
            rows.Add(new MessageLogDisplayRow(
                line.Category,
                display,
                line.SequenceId,
                line.UnitId,
                MessageLogCategoryClassMap.CssClassFor(line.Category)));
        }

        return new MessageLogPanelState(rows, source, viewMode);
    }

    /// <summary>
    /// Read-only export of the bound panel's underlying projection order via
    /// <see cref="MessageLogTextExport"/>. Reconstructs export text from row fields so hosts
    /// can export without holding the original <see cref="MessageLogLine"/> list.
    /// Does not mutate sim state.
    /// </summary>
    public static string ExportText(MessageLogPanelState panel)
    {
        if (panel is null)
        {
            throw new ArgumentNullException(nameof(panel));
        }

        if (panel.ViewMode == MessageLogPanelViewMode.Raw)
        {
            if (panel.Rows.Count == 0)
            {
                return string.Empty;
            }

            var builder = new System.Text.StringBuilder();
            foreach (var row in panel.Rows)
            {
                builder.Append(row.DisplayLine);
                builder.Append('\n');
            }

            return builder.ToString();
        }

        // Categorized rows store "[CATEGORY] text"; rebuild MessageLogLine-shaped export from fields.
        // Sim time is not retained on display rows — prefer ExportText(IReadOnlyList<MessageLogLine>)
        // or MessageLogTextExport.Render for AALog / golden fidelity.
        var lines = new List<MessageLogLine>(panel.Rows.Count);
        foreach (var row in panel.Rows)
        {
            var message = StripCategorizedPrefix(row.Category, row.DisplayLine);
            lines.Add(new MessageLogLine(row.SequenceId, 0, row.Category, message, row.UnitId));
        }

        return MessageLogTextExport.Render(lines);
    }

    /// <summary>
    /// Export path that preserves sim time: render from the original projected lines.
    /// Preferred for golden / AALog fidelity.
    /// </summary>
    public static string ExportText(IReadOnlyList<MessageLogLine> lines) =>
        MessageLogTextExport.Render(lines);

    /// <summary>Write export to a user path from projected lines (read-only w.r.t. sim).</summary>
    public static void WriteExport(IReadOnlyList<MessageLogLine> lines, string path) =>
        MessageLogTextExport.WriteToPath(lines, path);

    private static string FormatCategorized(string category, string text) => $"[{category}] {text}";

    private static string StripCategorizedPrefix(string category, string displayLine)
    {
        var prefix = $"[{category}] ";
        return displayLine.StartsWith(prefix, StringComparison.Ordinal)
            ? displayLine[prefix.Length..]
            : displayLine;
    }
}
