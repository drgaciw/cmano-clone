namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Controllers;
using Core;
using Projection;
using ProjectAegis.Sim.Scenario;
using Targets;

/// <summary>
/// UAT D22 side guard: decides whether a unit belongs to the opposing force (never player-commandable)
/// using the side source of truth, NOT agent naming. Lives outside <see cref="DelegationBridge"/> (zero-touch
/// invariant) and uses only its public API.
/// </summary>
/// <remarks>
/// Side source: <see cref="BalticV3SideRegistry.IsRedForceUnit"/> (the same registry
/// <c>MapPictureProjection</c> uses for hostile affiliation; covers legacy <c>hostile-1</c>/<c>ucav-red</c>
/// and scenario-registered red catalog units). A unit explicitly marked in the friendly mesh by
/// <see cref="DelegationBridge.ConfigureSimulationMode"/> is always treated as the player's side.
/// Delegation is irrelevant: a player-side unit under any agent id stays commandable.
/// </remarks>
public static class PlayerSideGuard
{
    /// <summary>
    /// True when <paramref name="entity"/> is on the opposing (red) side, is AI-driven, and is not under
    /// direct human control (a suspended agent means the human took over).
    /// </summary>
    public static bool IsOpposingAiUnit(DelegationBridge bridge, EntityKey entity)
    {
        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding)
            || binding.Target.Slot.Active is not AgentController)
        {
            return false;
        }

        if (IsMarkedFriendly(bridge, binding.TargetId))
        {
            return false;
        }

        return BalticV3SideRegistry.IsRedForceUnit(binding.TargetId.Value);
    }

    /// <summary>
    /// True when the player may command <paramref name="entity"/>: human-controlled, a suspended agent,
    /// or any agent-delegated unit that is not on the opposing side.
    /// </summary>
    public static bool IsPlayerControlledUnit(DelegationBridge bridge, EntityKey entity)
    {
        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            return false;
        }

        var slot = binding.Target.Slot;
        return slot.Active is HumanController
            || slot.SuspendedAgent is not null
            || (slot.Active is AgentController && !IsOpposingAiUnit(bridge, entity));
    }

    /// <summary>Attack menu for <paramref name="unitId"/>; every row disabled with NOT_PLAYER_CONTROLLED for opposing AI units.</summary>
    public static IReadOnlyList<EngageAttackOptions.AttackOption> GetAttackMenuOptions(
        DelegationBridge bridge,
        string unitId,
        ISimWorldSnapshot snapshot)
    {
        var options = bridge.GetAttackMenuOptions(unitId, snapshot);
        if (!TryFindEntity(bridge, unitId, out var entity) || !IsOpposingAiUnit(bridge, entity))
        {
            return options;
        }

        var denied = new List<EngageAttackOptions.AttackOption>(options.Count);
        foreach (var option in options)
        {
            denied.Add(option with
            {
                Enabled = false,
                DisabledReason = C2PlayerCommandBridge.ReasonNotPlayerControlled,
            });
        }

        return denied;
    }

    /// <summary>Guarded <see cref="DelegationBridge.TryEnqueueAttackOption"/>: refuses opposing AI units without taking control.</summary>
    public static bool TryEnqueueAttackOption(
        DelegationBridge bridge,
        EntityKey entity,
        string optionId,
        ISimWorldSnapshot snapshot,
        out string? failureReason)
    {
        if (IsOpposingAiUnit(bridge, entity))
        {
            failureReason = C2PlayerCommandBridge.ReasonNotPlayerControlled;
            return false;
        }

        return bridge.TryEnqueueAttackOption(entity, optionId, snapshot, out failureReason);
    }

    private static bool IsMarkedFriendly(DelegationBridge bridge, TargetId id)
    {
        foreach (var marked in bridge.Registry.CollectFriendlyMeshMemberIds())
        {
            if (marked.Equals(id))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindEntity(DelegationBridge bridge, string unitId, out EntityKey entity)
    {
        foreach (var binding in bridge.Registry.Bindings)
        {
            if (string.Equals(binding.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                entity = binding.Entity;
                return true;
            }
        }

        entity = default;
        return false;
    }
}
