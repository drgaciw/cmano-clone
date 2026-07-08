using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Outcome of fanning a group order out to <see cref="GroupOrderPlan.EligibleUnitIds"/>.</summary>
public sealed record GroupOrderFanOutResult(
    IReadOnlyList<string> Dispatched,
    IReadOnlyList<string> Failed)
{
    public static readonly GroupOrderFanOutResult Empty =
        new(Array.Empty<string>(), Array.Empty<string>());
}

/// <summary>
/// Thin executor over a <see cref="GroupOrderPlan"/>: issues exactly one existing-bridge call per
/// eligible unit (<see cref="DelegationBridge.TryEnqueueAttackOption"/> /
/// <see cref="DelegationBridge.TryEnqueueHumanOrder"/>) — no new bridge surface, ZERO diff to
/// <c>DelegationBridge.cs</c> (req 20 §Selection, TR-c2-005). Units that no longer resolve to a
/// registered entity (e.g. destroyed since the plan was built) are reported as failed rather than
/// throwing, matching the GDD edge case: dropped with a log note, fan-out continues for survivors.
/// </summary>
public static class GroupOrderFanOut
{
    /// <summary>Fan an attack-menu option (req 14 / doc 20 attack menu) out to every eligible unit.</summary>
    public static GroupOrderFanOutResult ExecuteAttackOption(
        GroupOrderPlan plan,
        DelegationBridge bridge,
        ISimWorldSnapshot snapshot,
        string optionId)
    {
        if (plan == null || bridge == null || snapshot == null)
        {
            return GroupOrderFanOutResult.Empty;
        }

        var dispatched = new List<string>();
        var failed = new List<string>();

        foreach (var unitId in plan.EligibleUnitIds)
        {
            if (!TryResolveEntity(bridge, unitId, out var entity))
            {
                failed.Add(unitId);
                continue;
            }

            if (bridge.TryEnqueueAttackOption(entity, optionId, snapshot, out _))
            {
                dispatched.Add(unitId);
            }
            else
            {
                failed.Add(unitId);
            }
        }

        return new GroupOrderFanOutResult(dispatched, failed);
    }

    /// <summary>Fan a non-attack human order (Move/Hold/SetEwPosture/ReturnToBase) out to every eligible unit.</summary>
    public static GroupOrderFanOutResult ExecuteHumanOrder(
        GroupOrderPlan plan,
        DelegationBridge bridge,
        OrderKind kind,
        double simTime,
        RiskLevel? risk = null)
    {
        if (plan == null || bridge == null)
        {
            return GroupOrderFanOutResult.Empty;
        }

        var dispatched = new List<string>();
        var failed = new List<string>();

        foreach (var unitId in plan.EligibleUnitIds)
        {
            if (!TryResolveEntity(bridge, unitId, out var entity))
            {
                failed.Add(unitId);
                continue;
            }

            if (bridge.TryEnqueueHumanOrder(entity, kind, simTime, risk))
            {
                dispatched.Add(unitId);
            }
            else
            {
                failed.Add(unitId);
            }
        }

        return new GroupOrderFanOutResult(dispatched, failed);
    }

    private static bool TryResolveEntity(DelegationBridge bridge, string unitId, out EntityKey entity)
    {
        entity = default;
        foreach (var binding in bridge.Registry.Bindings)
        {
            if (string.Equals(binding.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                entity = binding.Entity;
                return true;
            }
        }

        return false;
    }
}
