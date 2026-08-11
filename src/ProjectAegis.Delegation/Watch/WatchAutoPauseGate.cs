namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// S115-03: decides whether a newly enqueued pause-class event should auto-pause the sim,
/// and gates resume when unresolved pause-class cards remain.
/// Does not own the clock — callers invoke <c>PauseSim</c> / <c>ResumeSim</c> on the session.
/// HeadlessBatch override remains the responsibility of <c>SimTickPipeline</c> (already implemented).
/// </summary>
public sealed class WatchAutoPauseGate
{
    private WatchPauseReason _lastReason = WatchPauseReason.None;

    /// <summary>Most recent auto-pause reason (or <see cref="WatchPauseReason.None"/>).</summary>
    public WatchPauseReason LastPauseReason => _lastReason;

    /// <summary>
    /// After a successful enqueue of a pause-class event, returns true if the session
    /// should call PauseSim. Sets <see cref="LastPauseReason"/>.
    /// </summary>
    public bool ShouldAutoPause(WatchAttentionEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (!evt.IsPauseClass)
        {
            return false;
        }

        _lastReason = evt.Kind switch
        {
            WatchAttentionKind.HostileOrUnknownContact => WatchPauseReason.HostileOrUnknownContact,
            WatchAttentionKind.OwnSideLossOrDamage => WatchPauseReason.OwnSideLossOrDamage,
            _ => WatchPauseReason.None,
        };

        return _lastReason != WatchPauseReason.None;
    }

    /// <summary>
    /// Resume is allowed when there are zero unresolved pause-class cards,
    /// or when <paramref name="explicitOverride"/> is true (player force-resume).
    /// </summary>
    public bool CanResume(WatchAttentionQueue queue, bool explicitOverride)
    {
        ArgumentNullException.ThrowIfNull(queue);
        if (explicitOverride)
        {
            return true;
        }

        return !queue.HasUnresolvedPauseClass;
    }

    /// <summary>Clears the stored reason (e.g. after a clean resume).</summary>
    public void ClearReason()
    {
        _lastReason = WatchPauseReason.None;
    }
}
