namespace ProjectAegis.Delegation.Sim;

using Core;

/// <summary>Authoritative state observed at one simulation time.</summary>
/// <param name="SimTime">Current simulation time.</param>
/// <param name="ContactCount">Number of observed contacts.</param>
/// <param name="ActiveEngagementCount">Number of active engagements.</param>
/// <param name="MemberAlive">Alive state keyed by member target id.</param>
/// <param name="HasFireControlTrack">Whether a usable fire-control track exists.</param>
/// <param name="PrimaryHostileContactId">Optional primary hostile contact.</param>
/// <param name="RadarEmconActive">Whether radar emission control is active.</param>
/// <param name="PrimaryHostileDestroyed">Whether the primary hostile is destroyed.</param>
/// <param name="PrimaryBlueForceContactId">Optional primary blue-force contact.</param>
/// <param name="PrimaryBlueForceContactDestroyed">Whether the primary blue-force contact is destroyed.</param>
/// <param name="PreferredHostileByShooter">Optional multi-domain map from shooter platform id to preferred hostile platform id. Detection trials populate it so concurrent air, surface, and subsurface engagements do not collapse onto one target.</param>
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
