namespace ProjectAegis.Delegation.Projection;

/// <summary>UI Toolkit–friendly message log rows (GDD order-log-replay §3, requirements C2).</summary>
public sealed record MessageLogPanelState(IReadOnlyList<MessageLogDisplayRow> Rows);

/// <summary>Display row with order-log deep-link fields for CMD-05 selection.</summary>
public sealed record MessageLogDisplayRow(
    string Category,
    string DisplayLine,
    ulong SequenceId = 0,
    string? UnitId = null);