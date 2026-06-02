namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

public static class UnitDetailBridge
{
    public static UnitDetailEntry? BuildPrimary(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = "u1") =>
        UnitDetailProjection.ProjectPrimary(
            registry.CollectMemberIds(),
            snapshot.IsMemberAlive,
            log,
            policy,
            observerUnitId);

    public static UnitDetailEntry? BuildSelected(
        TargetId unitId,
        ISimWorldSnapshot snapshot,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = "u1") =>
        UnitDetailProjection.ProjectSelected(
            unitId,
            snapshot.IsMemberAlive,
            log,
            policy,
            observerUnitId);
}