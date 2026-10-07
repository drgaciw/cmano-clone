namespace ProjectAegis.Delegation.UnityAdapter.PlayDelegation;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;

/// <summary>One commanded target row in the Assign Agent / Rebrief panel.</summary>
public sealed record AssignAgentRowPresentation(
    string TargetId,
    PlaySide Side,
    string ControllerKind,
    string? AgentId,
    string? PersonalitySlug,
    AutonomyLevel? Autonomy,
    bool CanAssign,
    bool CanRebrief,
    string? RebriefBlockedReason);

/// <summary>
/// Assign Agent / Rebrief panel model (S124-05 W2-DEL-04, S124-06 W2-DEL-02). Read-only projection of the
/// session's commanded targets and the <see cref="LoopPolicyGate.CanRebriefAgent"/> verdict; actions go
/// through <see cref="PlayDelegationCommands"/>. Hosted apart from the commit strip.
/// </summary>
public sealed record AssignAgentPanelPresentation(
    bool IsAvailable,
    string? UnavailableReason,
    IReadOnlyList<string> Presets,
    IReadOnlyList<AssignAgentRowPresentation> Rows)
{
    public static AssignAgentPanelPresentation Project(PlayEntrySession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        if (session.Package == null || session.Bridge == null)
        {
            return Unavailable(PlayEntryErrorCodes.NoPackage);
        }

        var orchestrator = session.Bridge.Orchestrator;
        if (orchestrator.Phase != SimulationPhase.Executing)
        {
            return Unavailable(PlayDelegationErrorCodes.NotExecuting);
        }

        var gate = LoopPolicyGate.CanRebriefAgent(orchestrator.ScenarioPolicy, orchestrator.Phase);
        var viewer = orchestrator.AttachReplayViewer;
        var commanded = session.CommandedTargets;
        var rows = new AssignAgentRowPresentation[commanded.Count];
        for (var i = 0; i < commanded.Count; i++)
        {
            rows[i] = ProjectRow(commanded[i], gate, viewer);
        }

        return new AssignAgentPanelPresentation(true, null, PresetNames(), rows);
    }

    private static AssignAgentRowPresentation ProjectRow(PlayCommandedTarget commanded, LoopPolicyVerdict gate, bool viewer)
    {
        var slot = commanded.Target.Slot;
        var agent = slot.Active as AgentController;
        var kind = slot.Active switch
        {
            HumanController => "Human",
            AgentController => "Agent",
            null when slot.SuspendedAgent is not null => "AgentSuspended",
            _ => "None",
        };
        var canAssign = !viewer && agent == null && slot.SuspendedAgent == null;
        var rebriefBlocked = agent == null
            ? PlayDelegationErrorCodes.NoAgent
            : gate.DenialReason;

        return new AssignAgentRowPresentation(
            commanded.Target.Id.Value,
            commanded.Side,
            kind,
            agent?.Id.Value,
            agent?.PersonalitySlug,
            agent?.Autonomy,
            canAssign,
            !viewer && rebriefBlocked == null,
            rebriefBlocked);
    }

    private static AssignAgentPanelPresentation Unavailable(string reason) =>
        new(false, reason, Array.Empty<string>(), Array.Empty<AssignAgentRowPresentation>());

    private static IReadOnlyList<string> PresetNames()
    {
        var names = new string[PersonalityCatalog.All.Count];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = PersonalityCatalog.All[i].Name;
        }

        return names;
    }
}
