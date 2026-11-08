namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Decision;
using Projection;

/// <summary>
/// Headless/Unity facade: project order log to HUD message lines.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
/// <remarks>
/// Live vs full binding (DRG-248, req 02): the live HUD must bind <see cref="ProjectLive"/> /
/// <see cref="BindLive"/>, which follow <see cref="DelegationBridge.GetLiveOrderLogView"/> and so
/// omit agent decisions hidden by <c>delegationFog</c> / <c>tieredByAutonomy</c>. Replay / AAR
/// binds <see cref="ProjectReplay"/> / <see cref="BindReplay"/> (and the legacy
/// <see cref="ProjectFrom"/>), which read the complete <see cref="DecisionLog"/>.
/// </remarks>
public static class MessageLogBridge
{
    /// <summary>
    /// Full AAR message log (all projected order-log categories) for presentation bind.
    /// Consumes <see cref="DecisionLog"/> only — no live ECS / session write handles.
    /// Not player-info filtered; prefer <see cref="ProjectLive"/> for the live HUD.
    /// </summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="MessageLogLine"/> rows.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectFrom(DecisionLog log)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return MessageLogProjection.Project(log);
    }

    /// <summary>Replay / AAR message log: the complete <see cref="DecisionLog"/>, unfiltered.</summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Immutable list of every projected message line.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectReplay(DecisionLog log) => ProjectFrom(log);

    /// <summary>
    /// Live HUD message log: projects <see cref="DelegationBridge.GetLiveOrderLogView"/>, honoring the
    /// scenario <c>PlayerInfoModel</c>. Read-only; does not tick or mutate the bridge.
    /// </summary>
    /// <param name="bridge">Bridge whose live order-log view is projected.</param>
    /// <returns>Immutable list of message lines visible to the player live.</returns>
    /// <exception cref="ArgumentNullException">When bridge is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectLive(DelegationBridge bridge)
    {
        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        return MessageLogProjection.Project(bridge.GetLiveOrderLogView());
    }

    /// <summary>Bind the live HUD panel (<see cref="MessageLogBindingSource.LiveOrderLogView"/>).</summary>
    /// <param name="bridge">Bridge whose live order-log view is projected.</param>
    /// <returns>Panel state tagged as live.</returns>
    /// <exception cref="ArgumentNullException">When bridge is null.</exception>
    public static MessageLogPanelState BindLive(DelegationBridge bridge) =>
        MessageLogPanelBinder.Bind(ProjectLive(bridge), MessageLogBindingSource.LiveOrderLogView);

    /// <summary>Bind the replay / AAR panel (<see cref="MessageLogBindingSource.FullDecisionLog"/>).</summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Panel state tagged as full log.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static MessageLogPanelState BindReplay(DecisionLog log) =>
        MessageLogPanelBinder.Bind(ProjectReplay(log), MessageLogBindingSource.FullDecisionLog);

    /// <summary>
    /// Compact combat strip (bottom HUD subset: kills, intercepts, hits, misses, magazine).
    /// </summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Immutable combat-category subset of message lines.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectCombatMessages(DecisionLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return ProjectFrom(log)
            .Where(m => m.Category is "KILL_CONFIRMED" or "INTERCEPT_SUCCESS" or "HIT" or "MISS" or "MAGAZINE")
            .ToArray();
    }
}
