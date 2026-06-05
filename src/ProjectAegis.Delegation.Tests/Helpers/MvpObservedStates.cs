namespace ProjectAegis.Delegation.Tests.Helpers;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;

internal static class MvpObservedStates
{
    public static ObservedState EngageTick(
        double simTime,
        int contactCount = 2,
        bool hasFireControlTrack = true,
        bool radarEmconActive = true,
        TargetId? primaryHostile = null) =>
        new(
            simTime,
            contactCount,
            ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(),
            hasFireControlTrack,
            primaryHostile ?? new TargetId("hostile-1"),
            radarEmconActive);
}