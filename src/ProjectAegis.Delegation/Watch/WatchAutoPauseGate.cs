namespace ProjectAegis.Delegation.Watch;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;

/// <summary>
/// S115-03 / AEGIS-301: decides whether a newly enqueued pause-class event or pending approval
/// order proposal should auto-pause the sim, and gates resume when unresolved pause-class cards
/// or pending order proposals remain.
/// Does not own the clock — callers invoke <c>PauseSim</c> / <c>ResumeSim</c> on the session.
/// HeadlessBatch override remains the responsibility of <c>SimTickPipeline</c> (already implemented).
/// </summary>
public sealed class WatchAutoPauseGate
{
    private WatchPauseReason _lastReason = WatchPauseReason.None;

    /// <summary>Master toggle for auto-pause evaluation. Defaults to true.</summary>
    public bool AutoPauseEnabled { get; set; } = true;

    /// <summary>Alias for <see cref="AutoPauseEnabled"/>.</summary>
    public bool IsAutoPauseEnabled
    {
        get => AutoPauseEnabled;
        set => AutoPauseEnabled = value;
    }

    /// <summary>Optional connected <see cref="PendingApprovalQueue"/>.</summary>
    public PendingApprovalQueue? PendingApprovalQueue { get; set; }

    /// <summary>Most recent auto-pause reason (or <see cref="WatchPauseReason.None"/>).</summary>
    public WatchPauseReason LastPauseReason => _lastReason;

    /// <summary>
    /// After a successful enqueue of a pause-class event, returns true if the session
    /// should call PauseSim. Sets <see cref="LastPauseReason"/>.
    /// </summary>
    public bool ShouldAutoPause(WatchAttentionEvent evt)
    {
        if (!AutoPauseEnabled)
        {
            return false;
        }

        // netstandard2.1: ArgumentNullException.ThrowIfNull is net5+ only.
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

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
    /// Evaluates whether the gate should auto-pause for pending approvals in the given queue.
    /// Returns true and sets <see cref="LastPauseReason"/> to <see cref="WatchPauseReason.OrderProposal"/>
    /// if auto-pause is enabled and the queue contains pending proposals.
    /// </summary>
    public bool ShouldAutoPause(PendingApprovalQueue? queue)
    {
        if (!AutoPauseEnabled || queue is null || !queue.HasPendingProposals)
        {
            return false;
        }

        _lastReason = WatchPauseReason.OrderProposal;
        return true;
    }

    /// <summary>
    /// Evaluates whether the gate should auto-pause for an enqueued order proposal.
    /// Sets <see cref="LastPauseReason"/> to <see cref="WatchPauseReason.OrderProposal"/>.
    /// </summary>
    public bool ShouldAutoPause(PendingApprovalEntry? entry)
    {
        if (!AutoPauseEnabled || entry is null)
        {
            return false;
        }

        _lastReason = WatchPauseReason.OrderProposal;
        return true;
    }

    /// <summary>
    /// Evaluates whether the gate should auto-pause for an enqueued order.
    /// Sets <see cref="LastPauseReason"/> to <see cref="WatchPauseReason.OrderProposal"/>.
    /// </summary>
    public bool ShouldAutoPause(Order? order)
    {
        if (!AutoPauseEnabled || order is null)
        {
            return false;
        }

        _lastReason = WatchPauseReason.OrderProposal;
        return true;
    }

    /// <summary>
    /// Evaluates whether the connected <see cref="PendingApprovalQueue"/> has pending proposals that should trigger auto-pause.
    /// </summary>
    public bool ShouldAutoPausePendingApprovals() => ShouldAutoPause(PendingApprovalQueue);

    /// <summary>
    /// Resume is allowed when there are zero unresolved pause-class cards,
    /// or when <paramref name="explicitOverride"/> is true (player force-resume).
    /// </summary>
    public bool CanResume(WatchAttentionQueue queue, bool explicitOverride)
    {
        // netstandard2.1: ArgumentNullException.ThrowIfNull is net5+ only.
        if (queue is null)
        {
            throw new ArgumentNullException(nameof(queue));
        }

        if (explicitOverride)
        {
            return true;
        }

        return !queue.HasUnresolvedPauseClass;
    }

    /// <summary>
    /// Resume is allowed when there are zero unresolved pause-class cards and zero pending proposals,
    /// or when <paramref name="explicitOverride"/> is true (player force-resume).
    /// </summary>
    public bool CanResume(WatchAttentionQueue? queue, PendingApprovalQueue? pendingQueue, bool explicitOverride = false)
    {
        if (explicitOverride)
        {
            return true;
        }

        if (queue is not null && queue.HasUnresolvedPauseClass)
        {
            return false;
        }

        var pq = pendingQueue ?? PendingApprovalQueue;
        if (pq is not null && pq.HasPendingProposals)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resume is allowed when there are zero pending proposals in the queue,
    /// or when <paramref name="explicitOverride"/> is true.
    /// </summary>
    public bool CanResume(PendingApprovalQueue? pendingQueue, bool explicitOverride = false)
    {
        if (explicitOverride)
        {
            return true;
        }

        var pq = pendingQueue ?? PendingApprovalQueue;
        if (pq is not null && pq.HasPendingProposals)
        {
            return false;
        }

        return true;
    }

    /// <summary>Clears the stored reason (e.g. after a clean resume).</summary>
    public void ClearReason()
    {
        _lastReason = WatchPauseReason.None;
    }
}
