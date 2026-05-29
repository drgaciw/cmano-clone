namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Delegation.Core;

public sealed record ObservedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount,
    IReadOnlyDictionary<TargetId, bool> MemberAlive);

public sealed record PerceivedState(
    double SimTime,
    int ContactCount,
    int ActiveEngagementCount);

public static class PerceivedStateFactory
{
    public static PerceivedState FromFull(ObservedState full, double situationalAwareness)
    {
        var factor = Math.Clamp(situationalAwareness, 0, 1);
        var contacts = (int)Math.Round(full.ContactCount * factor);
        var engagements = (int)Math.Round(full.ActiveEngagementCount * factor);
        return new PerceivedState(full.SimTime, contacts, engagements);
    }
}
