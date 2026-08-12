namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Headless/Unity facade: unit detail panel rows from snapshot alive-state + decision log + policy.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// Overloads that accept <see cref="DelegationBridge"/> only call existing APIs (attack menu);
/// they do not edit the bridge hotpath.
/// </summary>
public static class UnitDetailBridge
{
    /// <summary>
    /// Projects the primary (observer) unit detail entry for presentation bind.
    /// </summary>
    /// <exception cref="ArgumentNullException">When snapshot, registry, or log is null.</exception>
    public static UnitDetailEntry? BuildPrimary(
        ISimWorldSnapshot snapshot,
        TargetRegistry registry,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = "u1")
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return UnitDetailProjection.ProjectPrimary(
            registry.CollectMemberIds(),
            snapshot.IsMemberAlive,
            log,
            policy,
            snapshot.SimTime,
            observerUnitId);
    }

    /// <summary>
    /// Projects primary unit detail and enriches attack menu via existing bridge APIs (no hotpath edit).
    /// </summary>
    /// <exception cref="ArgumentNullException">When snapshot or bridge is null.</exception>
    public static UnitDetailEntry? BuildPrimary(
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        string? observerUnitId = "u1")
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        return EnrichAttackMenu(
            BuildPrimary(
                snapshot,
                bridge.Registry,
                bridge.Orchestrator.DecisionLog,
                bridge.Orchestrator.ScenarioPolicy,
                observerUnitId),
            snapshot,
            bridge);
    }

    /// <summary>
    /// Projects a selected unit detail entry for presentation bind.
    /// </summary>
    /// <exception cref="ArgumentNullException">When snapshot or log is null.</exception>
    public static UnitDetailEntry? BuildSelected(
        TargetId unitId,
        ISimWorldSnapshot snapshot,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = "u1")
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return UnitDetailProjection.ProjectSelected(
            unitId,
            snapshot.IsMemberAlive,
            log,
            policy,
            snapshot.SimTime,
            observerUnitId);
    }

    /// <summary>
    /// Projects selected unit detail and enriches attack menu via existing bridge APIs (no hotpath edit).
    /// </summary>
    /// <exception cref="ArgumentNullException">When snapshot or bridge is null.</exception>
    public static UnitDetailEntry? BuildSelected(
        TargetId unitId,
        ISimWorldSnapshot snapshot,
        DelegationBridge bridge,
        string? observerUnitId = "u1")
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        return EnrichAttackMenu(
            BuildSelected(
                unitId,
                snapshot,
                bridge.Orchestrator.DecisionLog,
                bridge.Orchestrator.ScenarioPolicy,
                observerUnitId),
            snapshot,
            bridge,
            unitId.Value);
    }

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
