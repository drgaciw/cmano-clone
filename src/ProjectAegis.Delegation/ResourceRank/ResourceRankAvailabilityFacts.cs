namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>Magazine commitment and availability facts for scarcity-aware ranking.</summary>
public sealed record ResourceRankAvailabilityFacts(
    int RoundsCommittedElsewhere = 0,
    bool MountAvailable = true,
    double TimeToEffectSeconds = 0)
{
    /// <summary>Default availability — no external commitment.</summary>
    public static ResourceRankAvailabilityFacts None { get; } = new();
}
