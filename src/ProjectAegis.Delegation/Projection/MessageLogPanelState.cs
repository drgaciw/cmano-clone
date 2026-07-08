namespace ProjectAegis.Delegation.Projection;

/// <summary>UI Toolkit–friendly message log rows (GDD order-log-replay §3, requirements C2).</summary>
public sealed record MessageLogPanelState(IReadOnlyList<MessageLogDisplayRow> Rows);

/// <summary>
/// <paramref name="LifecycleState"/> (T2, req 20 §Order lifecycle) is set only for <c>PLAYER_ORDER</c>
/// rows when an <see cref="OrderLifecycleProjection"/> lookup is supplied to
/// <see cref="MessageLogPanelBinder.Bind(System.Collections.Generic.IReadOnlyList{MessageLogLine},System.Collections.Generic.IReadOnlyDictionary{OrderLifecycleProjection.OrderKey,OrderLifecycleState}?)"/>;
/// otherwise <c>null</c> (no behavior change for existing callers).
/// </summary>
public sealed record MessageLogDisplayRow(
    string Category,
    string DisplayLine,
    OrderLifecycleState? LifecycleState = null);