namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;

public static class ObservedStateBuilder
{
    public static ObservedState Build(ISimWorldSnapshot snapshot, IReadOnlyList<TargetId> memberIds)
    {
        var alive = new Dictionary<TargetId, bool>(memberIds.Count);
        foreach (var memberId in memberIds)
        {
            alive[memberId] = snapshot.IsMemberAlive(memberId);
        }

        return new ObservedState(
            snapshot.SimTime,
            snapshot.ContactCount,
            snapshot.ActiveEngagementCount,
            alive,
            snapshot.HasFireControlTrackOnPrimaryContact,
            snapshot.PrimaryHostileContactId,
            snapshot.ObserverRadarEmconActive,
            snapshot.PrimaryHostileDestroyed,
            snapshot.PrimaryBlueForceContactId,
            snapshot.PrimaryBlueForceContactDestroyed);
    }
}
