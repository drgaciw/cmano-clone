namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// Ordering priority for the watch attention queue (lower ordinal = higher priority).
/// </summary>
public enum WatchAttentionPriority : byte
{
    Critical = 0,
    High = 1,
    Normal = 2,
    Low = 3,
}
