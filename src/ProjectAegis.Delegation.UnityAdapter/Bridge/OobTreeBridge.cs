namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Projection;

/// <summary>Headless/Unity facade: OOB tree from registry members + snapshot alive state.</summary>
public static class OobTreeBridge
{
    public static IReadOnlyList<OobTreeEntry> Build(ISimWorldSnapshot snapshot, TargetRegistry registry) =>
        OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
}