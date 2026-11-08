namespace ProjectAegis.Delegation.Projection;

/// <summary>Which order-log view a message log panel is bound to (DRG-248, req 02).</summary>
public enum MessageLogBindingSource
{
    /// <summary>
    /// Complete <c>DecisionLog</c>, unfiltered. Use for replay / AAR only; under
    /// <c>delegationFog</c> or <c>tieredByAutonomy</c> it includes agent decisions the player
    /// must not see live.
    /// </summary>
    FullDecisionLog = 0,

    /// <summary>
    /// <c>GetLiveOrderLogView()</c>: the order log filtered by the scenario <c>PlayerInfoModel</c>.
    /// Use for the live HUD.
    /// </summary>
    LiveOrderLogView = 1,
}

/// <summary>UI Toolkit–friendly message log rows (GDD order-log-replay §3, requirements C2).</summary>
/// <param name="Rows">Display rows in log order.</param>
/// <param name="Source">Order-log view the rows were projected from (live vs full).</param>
public sealed record MessageLogPanelState(
    IReadOnlyList<MessageLogDisplayRow> Rows,
    MessageLogBindingSource Source = MessageLogBindingSource.FullDecisionLog);

/// <summary>Display row with order-log deep-link fields for CMD-05 selection.</summary>
public sealed record MessageLogDisplayRow(
    string Category,
    string DisplayLine,
    ulong SequenceId = 0,
    string? UnitId = null,
    string? CategoryCssClass = null);
