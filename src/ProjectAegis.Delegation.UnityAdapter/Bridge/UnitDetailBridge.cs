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
            snapshot.SimTime,
            observerUnitId);

    public static UnitDetailEntry? BuildPrimary(
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        string? observerUnitId = "u1") =>
        EnrichAttackMenu(
            BuildPrimary(
                snapshot,
                bridge.Registry,
                bridge.Orchestrator.DecisionLog,
                bridge.Orchestrator.ScenarioPolicy,
                observerUnitId),
            snapshot,
            bridge);

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
            snapshot.SimTime,
            observerUnitId);

    public static UnitDetailEntry? BuildSelected(
        TargetId unitId,
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        string? observerUnitId = "u1") =>
        EnrichAttackMenu(
            BuildSelected(
                unitId,
                snapshot,
                bridge.Orchestrator.DecisionLog,
                bridge.Orchestrator.ScenarioPolicy,
                observerUnitId),
            snapshot,
            bridge,
            unitId.Value);

    private static UnitDetailEntry? EnrichAttackMenu(
        UnitDetailEntry? entry,
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        string? unitIdOverride = null)
    {
        if (entry == null)
        {
            return null;
        }

        var unitId = unitIdOverride ?? entry.UnitId;
        var liveMenu = bridge.GetAttackMenuOptions(unitId, snapshot);
        return entry with { AttackMenu = liveMenu };
    }
}