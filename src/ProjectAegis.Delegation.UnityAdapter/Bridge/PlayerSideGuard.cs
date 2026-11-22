namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Controllers;
using Core;
using Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// UAT D22 side guard for player commands to AI-controlled units.
/// </summary>
/// <remarks>
/// Simulation mode records the roster commanded by the selected player side.
/// Before sides are bound, <see cref="BalticV3SideRegistry.IsRedForceUnit"/> supplies legacy affiliation.
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

        return !IsPlayerSide(bridge.Registry, binding.TargetId);
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
        if (slot.Active is HumanController || slot.SuspendedAgent is not null)
        {
            return bridge.Registry.IsPlayerCommandTarget(binding.TargetId) != false;
        }

        if (slot.Active is AgentController && IsPlayerSide(bridge.Registry, binding.TargetId))
        {
            return true;
        }

        if (slot.Active is null &&
            binding.Target is UnitTarget unit &&
            bridge.Orchestrator.FindParentGroup(unit.Id) is { } parentGroup &&
            (parentGroup.Slot.Active is HumanController || parentGroup.Slot.Active is AgentController))
        {
            return IsPlayerSide(bridge.Registry, binding.TargetId);
        }

        return false;
    }

    /// <summary>Attack menu for <paramref name="unitId"/>; every row disabled with NOT_PLAYER_CONTROLLED for opposing AI units.</summary>
    public static IReadOnlyList<EngageAttackOptions.AttackOption> GetAttackMenuOptions(
        DelegationBridge bridge,
        string unitId,
        ISimWorldSnapshot snapshot)
    {
        var options = bridge.GetAttackMenuOptions(unitId, snapshot);
        if (!bridge.Registry.TryGetBinding(new TargetId(unitId), out var binding)
            || !IsOpposingAiUnit(bridge, binding.Entity))
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

    private static bool IsPlayerSide(TargetRegistry registry, TargetId targetId) =>
        registry.IsPlayerCommandTarget(targetId) ?? !BalticV3SideRegistry.IsRedForceUnit(targetId.Value);
}
