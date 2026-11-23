namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

using Bridge;
using Controllers;
using Input;
using Targets;

/// <summary>Read-only control-handoff and agent-pause state for the Right Unit Detail panel (DRG-247 / DEL-03).</summary>
public sealed record RightUnitControlState(
    string? UnitId,
    string ControllerKind,
    string? AgentId,
    string StatusLabel,
    bool IsHumanControlled,
    bool HasAgent,
    bool IsAgentActive,
    bool IsAgentPaused,
    bool HasSuspendedAgent,
    bool IsDetachedFromGroup,
    bool IsPlayerControlled,
    bool IsDisabled,
    string? DisabledReason,
    string? DisabledReasonCode,
    bool CanTakeControl,
    bool CanReturnToAgent,
    bool CanPauseAgent,
    bool CanResumeAgent)
{
    public static RightUnitControlState Empty { get; } = new(
        UnitId: null,
        ControllerKind: "None",
        AgentId: null,
        StatusLabel: "CONTROL: —",
        IsHumanControlled: false,
        HasAgent: false,
        IsAgentActive: false,
        IsAgentPaused: false,
        HasSuspendedAgent: false,
        IsDetachedFromGroup: false,
        IsPlayerControlled: false,
        IsDisabled: true,
        DisabledReason: "No unit selected.",
        DisabledReasonCode: AgentDirectiveIssuance.ReasonNoSelection,
        CanTakeControl: false,
        CanReturnToAgent: false,
        CanPauseAgent: false,
        CanResumeAgent: false);
}

/// <summary>
/// Projects control-handoff &amp; agent pause/resume availability purely from <see cref="DelegationBridge"/>
/// without mutating sim state or exposing <c>ProjectAegis.Sim</c> to Unity UI hosts (ADR-010).
/// </summary>
public static class RightUnitControlProjection
{
    public static RightUnitControlState Project(DelegationBridge? bridge, string? selectedUnitId)
    {
        if (bridge is null || string.IsNullOrWhiteSpace(selectedUnitId))
        {
            return RightUnitControlState.Empty;
        }

        if (!TryFindBinding(bridge, selectedUnitId, out var binding))
        {
            return RightUnitControlState.Empty with
            {
                UnitId = selectedUnitId,
                DisabledReason = $"Unit '{selectedUnitId}' not found.",
                DisabledReasonCode = C2PlayerCommandBridge.ReasonUnknownUnit,
            };
        }

        var slot = binding.Target.Slot;
        var unitTarget = binding.Target as UnitTarget;
        var isDetached = unitTarget?.IsDetachedFromGroup == true;
        var parentGroupAgent = FindParentGroupAgent(bridge, binding);

        var isHuman = slot.Active is HumanController;
        var activeAgent = slot.Active as AgentController;
        var isAgentActive = activeAgent is not null;
        var isAgentPaused = activeAgent?.IsActionsPaused == true;
        var hasSuspended = slot.SuspendedAgent is not null;
        var hasAgent = isAgentActive || hasSuspended || isDetached || parentGroupAgent is not null;

        var agentId = activeAgent?.Id.Value
            ?? slot.SuspendedAgent?.Id.Value
            ?? parentGroupAgent?.Id.Value;

        var controllerKind = slot.Active switch
        {
            HumanController => "Human",
            AgentController a when a.IsActionsPaused => "AgentPaused",
            AgentController => "Agent",
            null when slot.SuspendedAgent is not null => "AgentSuspended",
            null when parentGroupAgent is not null => "GroupAgent",
            _ => "None",
        };

        var observerChrome = ObserverModeChromePresentation.Project(bridge.AttachReplayViewer);
        if (observerChrome.IsObserver)
        {
            return new RightUnitControlState(
                UnitId: binding.TargetId.Value,
                ControllerKind: controllerKind,
                AgentId: agentId,
                StatusLabel: $"CONTROL: {FormatKind(controllerKind, agentId)} · {observerChrome.DisabledReasonText}",
                IsHumanControlled: isHuman,
                HasAgent: hasAgent,
                IsAgentActive: isAgentActive,
                IsAgentPaused: isAgentPaused,
                HasSuspendedAgent: hasSuspended,
                IsDetachedFromGroup: isDetached,
                IsPlayerControlled: PlayerSideGuard.IsPlayerControlledUnit(bridge, binding.Entity),
                IsDisabled: true,
                DisabledReason: observerChrome.DisabledReasonText,
                DisabledReasonCode: observerChrome.DisabledReasonCode,
                CanTakeControl: false,
                CanReturnToAgent: false,
                CanPauseAgent: false,
                CanResumeAgent: false);
        }

        if (!PlayerSideGuard.IsPlayerControlledUnit(bridge, binding.Entity))
        {
            const string opposingReason = "Opposing unit — player control disabled.";
            return new RightUnitControlState(
                UnitId: binding.TargetId.Value,
                ControllerKind: controllerKind,
                AgentId: agentId,
                StatusLabel: $"CONTROL: {FormatKind(controllerKind, agentId)} · {opposingReason}",
                IsHumanControlled: isHuman,
                HasAgent: hasAgent,
                IsAgentActive: isAgentActive,
                IsAgentPaused: isAgentPaused,
                HasSuspendedAgent: hasSuspended,
                IsDetachedFromGroup: isDetached,
                IsPlayerControlled: false,
                IsDisabled: true,
                DisabledReason: opposingReason,
                DisabledReasonCode: C2PlayerCommandBridge.ReasonNotPlayerControlled,
                CanTakeControl: false,
                CanReturnToAgent: false,
                CanPauseAgent: false,
                CanResumeAgent: false);
        }

        var canTakeControl = !isHuman && (isAgentActive || parentGroupAgent is not null);
        var canReturnToAgent = isHuman && (hasSuspended || isDetached);
        var canPauseAgent = isAgentActive && !isAgentPaused;
        var canResumeAgent = isAgentActive && isAgentPaused;

        return new RightUnitControlState(
            UnitId: binding.TargetId.Value,
            ControllerKind: controllerKind,
            AgentId: agentId,
            StatusLabel: $"CONTROL: {FormatKind(controllerKind, agentId)}",
            IsHumanControlled: isHuman,
            HasAgent: hasAgent,
            IsAgentActive: isAgentActive,
            IsAgentPaused: isAgentPaused,
            HasSuspendedAgent: hasSuspended,
            IsDetachedFromGroup: isDetached,
            IsPlayerControlled: true,
            IsDisabled: false,
            DisabledReason: null,
            DisabledReasonCode: null,
            CanTakeControl: canTakeControl,
            CanReturnToAgent: canReturnToAgent,
            CanPauseAgent: canPauseAgent,
            CanResumeAgent: canResumeAgent);
    }

    private static string FormatKind(string controllerKind, string? agentId) =>
        controllerKind switch
        {
            "AgentPaused" => string.IsNullOrEmpty(agentId) ? "Agent (Paused)" : $"Agent {agentId} (Paused)",
            "Agent" => string.IsNullOrEmpty(agentId) ? "Agent (Active)" : $"Agent {agentId} (Active)",
            "GroupAgent" => string.IsNullOrEmpty(agentId) ? "Group Agent" : $"Group Agent {agentId}",
            "Human" => "Human (Direct)",
            "AgentSuspended" => "Agent (Suspended)",
            _ => "None",
        };

    private static bool TryFindBinding(
        DelegationBridge bridge,
        string unitId,
        out SimEntityBinding binding)
    {
        foreach (var candidate in bridge.Registry.Bindings)
        {
            if (string.Equals(candidate.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                binding = candidate;
                return true;
            }
        }

        binding = null!;
        return false;
    }

    private static AgentController? FindParentGroupAgent(
        DelegationBridge bridge,
        SimEntityBinding unitBinding)
    {
        foreach (var candidate in bridge.Registry.Bindings)
        {
            if (candidate.Target is GroupTarget group &&
                group.Members.Contains(unitBinding.TargetId) &&
                group.Slot.Active is AgentController agent)
            {
                return agent;
            }
        }

        return null;
    }
}
