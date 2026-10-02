namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Controllers;
using Core;
using Targets;

/// <summary>
/// Side-aware friendly datalink mesh membership for COMMS / network-health chrome (DRG-190).
/// Prefers explicit marks from <see cref="DelegationBridge.ConfigureSimulationMode"/>; otherwise
/// infers friendly mesh units from controller assignment (never the complete OOB/registry sweep).
/// </summary>
public static class FriendlyMeshUnitIdsProjection
{
    /// <summary>
    /// Alive friendly-mesh unit ids for <see cref="C2Network.C2NetworkHealthProjector"/>.
    /// </summary>
    public static IReadOnlyList<string> Collect(TargetRegistry registry, ISimWorldSnapshot snapshot)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var marked = registry.CollectFriendlyMeshMemberIds();
        if (marked.Count > 0)
        {
            return ToAliveUnitIds(marked, snapshot);
        }

        var inferred = new List<string>();
        foreach (var binding in registry.Bindings)
        {
            if (binding.Target is not UnitTarget)
            {
                continue;
            }

            if (!snapshot.IsMemberAlive(binding.TargetId))
            {
                continue;
            }

            if (!IsFriendlyMeshMember(binding.Target))
            {
                continue;
            }

            inferred.Add(binding.TargetId.Value);
        }

        return inferred;
    }

    internal static bool IsFriendlyMeshMember(ICommandableTarget target)
    {
        if (IsExcludedHostileUnitId(target.Id.Value))
        {
            return false;
        }

        return target.Slot.Active switch
        {
            HumanController => true,
            AgentController agent => !IsOpposingAgent(agent),
            _ => false,
        };
    }

    private static bool IsOpposingAgent(AgentController agent)
    {
        var agentId = agent.Id.Value;
        return agentId.StartsWith("opp-", StringComparison.Ordinal)
            || agentId.StartsWith("a-red", StringComparison.Ordinal);
    }

    private static bool IsExcludedHostileUnitId(string unitId) =>
        unitId.StartsWith("hostile", StringComparison.OrdinalIgnoreCase)
        || unitId.StartsWith("ucav-red", StringComparison.OrdinalIgnoreCase);

    private static string[] ToAliveUnitIds(IReadOnlyList<TargetId> memberIds, ISimWorldSnapshot snapshot)
    {
        var ids = new List<string>(memberIds.Count);
        foreach (var memberId in memberIds)
        {
            if (!snapshot.IsMemberAlive(memberId))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(memberId.Value))
            {
                continue;
            }

            ids.Add(memberId.Value);
        }

        return ids.ToArray();
    }
}
