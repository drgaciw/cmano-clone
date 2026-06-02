namespace ProjectAegis.Delegation.Projection;

/// <summary>UI Toolkit–friendly message log rows (GDD order-log-replay §3, requirements C2).</summary>
public sealed record MessageLogPanelState(IReadOnlyList<MessageLogDisplayRow> Rows);

public sealed record MessageLogDisplayRow(string Category, string DisplayLine);