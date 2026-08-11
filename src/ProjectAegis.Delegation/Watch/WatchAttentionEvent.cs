namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// Immutable pause-class attention event. EventId must be stable for a given
/// (kind, subject, trigger context) so re-emission is idempotent in the queue.
/// </summary>
/// <param name="EventId">Stable deterministic identifier (caller-supplied).</param>
/// <param name="Kind">Pause-class kind.</param>
/// <param name="Priority">Queue ordering priority.</param>
/// <param name="TriggerTick">Sim tick at which the fact was first observed.</param>
/// <param name="SubjectId">Contact id, unit id, or other subject key.</param>
/// <param name="GroupingKey">Optional raid/formation grouping (data only; UI grouping is P1).</param>
/// <param name="ReasonDetail">Optional free-text detail for projections (not hashed).</param>
public sealed record WatchAttentionEvent(
    string EventId,
    WatchAttentionKind Kind,
    WatchAttentionPriority Priority,
    ulong TriggerTick,
    string SubjectId,
    string? GroupingKey = null,
    string? ReasonDetail = null)
{
    /// <summary>True when this event is a classic auto-pause class.</summary>
    public bool IsPauseClass =>
        Kind is WatchAttentionKind.HostileOrUnknownContact
            or WatchAttentionKind.OwnSideLossOrDamage;
}
