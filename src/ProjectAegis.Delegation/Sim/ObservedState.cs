namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;

public sealed record ObservedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount,
    IReadOnlyDictionary<TargetId, bool> MemberAlive,
    bool HasFireControlTrack = true,
    TargetId? PrimaryHostileContactId = null,
    bool RadarEmconActive = true,
    bool PrimaryHostileDestroyed = false,
    TargetId? PrimaryBlueForceContactId = null,
    bool PrimaryBlueForceContactDestroyed = false,
    /// <summary>
    /// Optional multi-domain map: shooter platformId → preferred hostile platformId
    /// (from detection trials). Enables concurrent air/surface/sub engage without
    /// SwarmSalvoDeconfliction collapsing all blues onto one victim.
    /// </summary>
    IReadOnlyDictionary<string, string>? PreferredHostileByShooter = null);

public sealed record PerceivedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount,
    bool PrimaryHostileDestroyed = false,
    bool PrimaryBlueForceContactDestroyed = false);

public static class PerceivedStateFactory
{
    public static PerceivedState FromFull(ObservedState full, double situationalAwareness)
    {
        var factor = Math.Clamp(situationalAwareness, 0, 1);
        var contacts = (int)Math.Round(full.ContactCount * factor);
        var engagements = (int)Math.Round(full.ActiveEngagementCount * factor);
        return new PerceivedState(
            full.SimTime,
            contacts,
            engagements,
            full.PrimaryHostileDestroyed,
            full.PrimaryBlueForceContactDestroyed);
    }
}
