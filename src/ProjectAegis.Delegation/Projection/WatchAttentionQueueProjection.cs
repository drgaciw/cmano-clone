namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Watch;

/// <summary>
/// S115-02: projects the live <see cref="WatchAttentionQueue"/> into a stable UI contract.
/// Unity hosts bind labels only — they never re-derive priority or pause class.
/// </summary>
public static class WatchAttentionQueueProjection
{
    /// <summary>
    /// Ordered visible cards (non-dismissed). Priority → tick → EventId already applied by the queue.
    /// </summary>
    public static IReadOnlyList<WatchAttentionCard> ProjectVisible(WatchAttentionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return queue.SnapshotVisible();
    }

    /// <summary>Unresolved pause-class count for badge / auto-pause gating UI.</summary>
    public static int ProjectUnresolvedCount(WatchAttentionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return queue.UnresolvedPauseClassCount;
    }

    /// <summary>Headless reason string for the current auto-pause (or empty).</summary>
    public static string ProjectPauseReasonLabel(WatchPauseReason reason) =>
        reason switch
        {
            WatchPauseReason.HostileOrUnknownContact => "Hostile / unknown contact",
            WatchPauseReason.OwnSideLossOrDamage => "Own-side loss / damage",
            WatchPauseReason.ExplicitPlayer => "Player pause",
            _ => string.Empty,
        };
}
