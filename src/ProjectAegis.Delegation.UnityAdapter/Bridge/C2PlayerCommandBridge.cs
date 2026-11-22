namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Controllers;
using Core;
using Input;
using Targets;

/// <summary>
/// Player command issuance facade over <see cref="DelegationBridge.TryEnqueueHumanOrder"/> (CMD-31)
/// and unit control handoff / agent pause-resume commands (DRG-247 / DEL-03).
/// Returns structured failure reasons for UI tooltips / status labels.
/// </summary>
public static class C2PlayerCommandBridge
{
    public const string ReasonReplayAttached = "REPLAY_ATTACHED";
    public const string ReasonNotHumanControl = "NOT_HUMAN_CONTROL";
    /// <summary>UAT D22: unit is not under player control (hostile / AI-only).</summary>
    public const string ReasonNotPlayerControlled = "NOT_PLAYER_CONTROLLED";
    public const string ReasonUnknownUnit = "UNKNOWN_UNIT";
    public const string ReasonEnqueueFailed = "ENQUEUE_FAILED";
    public const string ReasonNoAgent = AgentDirectiveIssuance.ReasonNoAgent;
    public const string ReasonNoActiveAgent = AgentDirectiveIssuance.ReasonNoActiveAgent;
    public const string ReasonNoSuspendedAgent = AgentDirectiveIssuance.ReasonNoSuspendedAgent;
    public const string ReasonAlreadyPaused = AgentDirectiveIssuance.ReasonAgentAlreadyPaused;
    public const string ReasonNotPaused = AgentDirectiveIssuance.ReasonAgentNotPaused;
    public const string ReasonAlreadyHuman = "ALREADY_HUMAN_CONTROL";
    public const string ReasonHandoffFailed = "CONTROL_HANDOFF_FAILED";

    /// <summary>C3-01 / DRG-390: unit is off its side's comms grid (manifest Doctrine code).</summary>
    public const string ReasonOffGrid = ProjectAegis.Sim.Glossary.AbortReasonCatalog.Doctrine.COMMS_OFF_GRID;

    /// <summary>
    /// Resolve <paramref name="commandId"/> and enqueue a human order for <paramref name="entity"/>.
    /// Does not mutate control slots — unit must already be under <see cref="HumanController"/>.
    /// </summary>
    public static bool TryIssue(
        DelegationBridge bridge,
        EntityKey entity,
        string commandId,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;

        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!C2CommandIssuance.TryResolve(commandId, out var kind, out var resolveReason))
        {
            failureReason = resolveReason ?? C2CommandIssuance.ReasonUnknownCommand;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (binding.Target.Slot.Active is not HumanController)
        {
            failureReason = ReasonNotHumanControl;
            return false;
        }

        // C3-01 / DRG-390: immediate feedback; the orchestrator also drops off-grid human orders at drain.
        if (bridge.Orchestrator.EvaluateOffGrid(binding.TargetId.Value, (ulong)Math.Max(0, (long)simTime)) != null)
        {
            failureReason = ReasonOffGrid;
            return false;
        }

        if (!bridge.TryEnqueueHumanOrder(entity, kind, simTime))
        {
            failureReason = ReasonEnqueueFailed;
            return false;
        }

        return true;
    }

    /// <summary>DRG-247: take direct player control of <paramref name="entity"/> (including group member detach).</summary>
    public static bool TryTakeControl(
        DelegationBridge bridge,
        EntityKey entity,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;
        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!PlayerSideGuard.IsPlayerControlledUnit(bridge, entity))
        {
            failureReason = ReasonNotPlayerControlled;
            return false;
        }

        if (binding.Target is not UnitTarget unit)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (unit.Slot.Active is HumanController)
        {
            failureReason = ReasonAlreadyHuman;
            return false;
        }

        if (!HasActiveAgentOrGroupAgent(bridge, unit))
        {
            failureReason = ReasonNoActiveAgent;
            return false;
        }

        if (!bridge.TryTakeDirectControl(entity, simTime))
        {
            failureReason = ReasonHandoffFailed;
            return false;
        }

        return true;
    }

    /// <summary>DRG-247: return direct player control of <paramref name="entity"/> back to its agent or parent group.</summary>
    public static bool TryReturnToAgent(
        DelegationBridge bridge,
        EntityKey entity,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;
        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!PlayerSideGuard.IsPlayerControlledUnit(bridge, entity))
        {
            failureReason = ReasonNotPlayerControlled;
            return false;
        }

        if (binding.Target is not UnitTarget unit)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (unit.Slot.Active is not HumanController)
        {
            failureReason = ReasonNotHumanControl;
            return false;
        }

        if (unit.Slot.SuspendedAgent is null && !unit.IsDetachedFromGroup)
        {
            failureReason = ReasonNoSuspendedAgent;
            return false;
        }

        if (!bridge.TryReleaseDirectControl(entity, simTime))
        {
            failureReason = ReasonHandoffFailed;
            return false;
        }

        return true;
    }

    /// <summary>DRG-247: suspend autonomous agent decisions for <paramref name="entity"/> without clearing tasking.</summary>
    public static bool TryPauseAgentActions(
        DelegationBridge bridge,
        EntityKey entity,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;
        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!PlayerSideGuard.IsPlayerControlledUnit(bridge, entity))
        {
            failureReason = ReasonNotPlayerControlled;
            return false;
        }

        if (binding.Target.Slot.Active is not AgentController agent)
        {
            failureReason = ReasonNoActiveAgent;
            return false;
        }

        if (agent.IsActionsPaused)
        {
            failureReason = ReasonAlreadyPaused;
            return false;
        }

        if (!bridge.Orchestrator.TryPauseAgentActions(binding.Target, simTime))
        {
            failureReason = ReasonHandoffFailed;
            return false;
        }

        return true;
    }

    /// <summary>DRG-247: resume suspended autonomous agent decisions for <paramref name="entity"/>.</summary>
    public static bool TryResumeAgentActions(
        DelegationBridge bridge,
        EntityKey entity,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;
        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!PlayerSideGuard.IsPlayerControlledUnit(bridge, entity))
        {
            failureReason = ReasonNotPlayerControlled;
            return false;
        }

        if (binding.Target.Slot.Active is not AgentController agent)
        {
            failureReason = ReasonNoActiveAgent;
            return false;
        }

        if (!agent.IsActionsPaused)
        {
            failureReason = ReasonNotPaused;
            return false;
        }

        if (!bridge.Orchestrator.TryResumeAgentActions(binding.Target, simTime))
        {
            failureReason = ReasonHandoffFailed;
            return false;
        }

        return true;
    }

    private static bool HasActiveAgentOrGroupAgent(DelegationBridge bridge, UnitTarget unit)
    {
        if (unit.Slot.Active is AgentController)
        {
            return true;
        }

        var parent = bridge.Orchestrator.FindParentGroup(unit.Id);
        return parent is not null && parent.Slot.Active is AgentController;
    }
}
