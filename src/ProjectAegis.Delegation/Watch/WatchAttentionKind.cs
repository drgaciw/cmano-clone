namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// S115 / PRD P0-6·P0-7: pause-class kinds that the watch officer must see.
/// Distinct from <c>Delegation.Attention</c> (AI cognitive-load model).
/// </summary>
public enum WatchAttentionKind : byte
{
    /// <summary>First detection of hostile or unknown contact (classic rule).</summary>
    HostileOrUnknownContact = 0,

    /// <summary>Own-side unit loss or battle-damage transition.</summary>
    OwnSideLossOrDamage = 1,
}
