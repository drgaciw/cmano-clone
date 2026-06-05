namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

public static class MapPictureBridge
{
    public static IReadOnlyList<MapSymbolEntry> Build(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        int layoutSeed)
    {
        _ = snapshot;
        var oob = OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
        var contacts = ContactPictureProjection.Project(log);
        return MapPictureProjection.Project(oob, contacts, layoutSeed);
    }
}