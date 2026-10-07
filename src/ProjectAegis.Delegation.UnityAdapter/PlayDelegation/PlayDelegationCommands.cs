namespace ProjectAegis.Delegation.UnityAdapter.PlayDelegation;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;

/// <summary>
/// Headless play-delegation command façade (S124-05 W2-DEL-04 Assign Agent, S124-06 W2-DEL-02 Rebrief Agent).
/// Validates against the <see cref="PlayEntrySession"/> commanded targets, then routes authority through
/// <see cref="DelegationOrchestrator.TryAssignAgentController"/> / <see cref="DelegationOrchestrator.TryRebriefAgent"/>,
/// which own the order-log writes (ADR-010 §2–3, ADR-001). <c>DelegationBridge.Orchestrator</c> is the
/// existing headless command seam (same shape as the replay harness); these calls do not enter
/// <c>DelegationBridge.Tick</c>. Map presentation stays read-only (ADR-007). Failed commands never mutate
/// controllers or the log.
/// </summary>
public sealed class PlayDelegationCommands
{
    private readonly PlayEntrySession _session;

    public PlayDelegationCommands(PlayEntrySession session) =>
        _session = session ?? throw new ArgumentNullException(nameof(session));

    /// <summary>Deterministic agent id for a player-assigned controller.</summary>
    public static AgentId AgentIdFor(TargetId targetId) => new($"player-{targetId.Value}");

    /// <summary>Resolve a personality preset by catalog name (ordinal); null when unknown.</summary>
    public static PersonalityPreset? FindPreset(string? presetName)
    {
        foreach (var preset in PersonalityCatalog.All)
        {
            if (string.Equals(preset.Name, presetName, StringComparison.Ordinal))
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>
    /// Assign a new agent controller with <paramref name="presetName"/> at <paramref name="autonomy"/> to a
    /// commanded target. Available only after Begin Execution, because Begin applies the mode's controllers.
    /// </summary>
    public PlayEntryResult TryAssignAgent(TargetId targetId, string presetName, AutonomyLevel autonomy, double simTime)
    {
        var blocked = RequireExecuting();
        if (blocked != null)
        {
            return blocked;
        }

        var commanded = FindCommanded(targetId);
        if (commanded == null)
        {
            return NotCommanded(targetId);
        }

        var preset = FindPreset(presetName);
        if (preset == null)
        {
            return UnknownPreset(presetName);
        }

        var orchestrator = _session.Bridge!.Orchestrator;
        var agent = orchestrator.CreateAgentFromPreset(AgentIdFor(targetId), preset, autonomy);
        var verdict = orchestrator.TryAssignAgentController(
            commanded.Target,
            agent,
            isFriendly: commanded.Side == PlaySide.Friendly,
            simTime);
        return verdict.Allowed
            ? PlayEntryResult.Ok($"Agent {agent.Id.Value} ({preset.Name}, {autonomy}) assigned to {targetId.Value}.")
            : PlayEntryResult.Fail(PlayDelegationErrorCodes.AssignDenied, verdict.DenialReason ?? "Assign Agent denied.");
    }

    /// <summary>Rebrief the agent controlling a commanded target onto <paramref name="presetName"/>.</summary>
    public PlayEntryResult TryRebriefAgent(TargetId targetId, string presetName, double simTime)
    {
        var blocked = RequireExecuting();
        if (blocked != null)
        {
            return blocked;
        }

        var commanded = FindCommanded(targetId);
        if (commanded == null)
        {
            return NotCommanded(targetId);
        }

        if (commanded.Target.Slot.Active is not AgentController agent)
        {
            return PlayEntryResult.Fail(
                PlayDelegationErrorCodes.NoAgent,
                $"{targetId.Value} has no active agent controller to rebrief.");
        }

        var preset = FindPreset(presetName);
        if (preset == null)
        {
            return UnknownPreset(presetName);
        }

        var previous = agent.PersonalitySlug ?? "custom";
        var verdict = _session.Bridge!.Orchestrator.TryRebriefAgent(agent, preset, simTime);
        return verdict.Allowed
            ? PlayEntryResult.Ok($"Agent {agent.Id.Value} rebriefed {previous} → {preset.Name}.")
            : PlayEntryResult.Fail(
                PlayDelegationErrorCodes.RebriefDenied,
                $"Rebrief denied for {agent.Id.Value}: {verdict.DenialReason}");
    }

    private PlayEntryResult? RequireExecuting()
    {
        if (_session.Package == null || _session.Bridge == null)
        {
            return PlayEntryResult.Fail(PlayEntryErrorCodes.NoPackage, "No scenario package loaded.");
        }

        return _session.Bridge.Phase == SimulationPhase.Executing
            ? null
            : PlayEntryResult.Fail(
                PlayDelegationErrorCodes.NotExecuting,
                "Assign / rebrief agents after Begin Execution; Begin applies the mode's controllers.");
    }

    private PlayCommandedTarget? FindCommanded(TargetId targetId)
    {
        foreach (var commanded in _session.CommandedTargets)
        {
            if (commanded.Target.Id == targetId)
            {
                return commanded;
            }
        }

        return null;
    }

    private static PlayEntryResult NotCommanded(TargetId targetId) =>
        PlayEntryResult.Fail(
            PlayDelegationErrorCodes.TargetNotCommanded,
            $"{targetId.Value} is not commanded by the player in this session.");

    private static PlayEntryResult UnknownPreset(string? presetName) =>
        PlayEntryResult.Fail(PlayDelegationErrorCodes.UnknownPreset, $"Unknown personality preset '{presetName}'.");
}
