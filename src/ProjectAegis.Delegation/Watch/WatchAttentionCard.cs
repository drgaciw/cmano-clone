namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// Presentation-facing card derived from a <see cref="WatchAttentionEvent"/>.
/// Acknowledge / dismiss are presentation-only and do not mutate sim policy.
/// </summary>
/// <param name="Event">Source event (immutable).</param>
/// <param name="IsAcknowledged">Player has acknowledged; still visible until dismissed or cleared.</param>
/// <param name="IsDismissed">Soft-removed from default queue view; restorable.</param>
public sealed record WatchAttentionCard(
    WatchAttentionEvent Event,
    bool IsAcknowledged = false,
    bool IsDismissed = false)
{
    public string EventId => Event.EventId;
    public WatchAttentionKind Kind => Event.Kind;
    public WatchAttentionPriority Priority => Event.Priority;
    public ulong TriggerTick => Event.TriggerTick;
    public string SubjectId => Event.SubjectId;
    public string? GroupingKey => Event.GroupingKey;
    public bool IsPauseClass => Event.IsPauseClass;
    public bool IsUnresolved => !IsAcknowledged && !IsDismissed && IsPauseClass;
}
