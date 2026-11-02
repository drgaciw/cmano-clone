namespace ProjectAegis.Delegation.UnityAdapter.PlayDelegation;

using Controllers;
using Core;
using Decision;
using Orchestration;
using Traits;
using PlayEntry;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Headless play-delegation command façade (S124-05 W2-DEL-04 Assign Agent, S124-06 W2-DEL-02 Rebrief Agent,
/// DRG-250 personality and autonomy edit). Validates against the <see cref="PlayEntrySession"/> commanded
/// targets, then routes authority through <see cref="DelegationOrchestrator"/> (ADR-010 §2–3, ADR-001).
/// <c>DelegationBridge.Orchestrator</c> is the existing headless command seam; these calls do not enter
/// <c>DelegationBridge.Tick</c>. Map presentation stays read-only (ADR-007). Failed commands never mutate
/// controllers or the log.
/// </summary>
public sealed class PlayDelegationCommands
{
    /// <summary>Result code when <see cref="LoopPolicyGate"/> denies a personality edit.</summary>
    public const string PersonalityDenied = "PERSONALITY_DENIED";

    /// <summary>Result code when <see cref="LoopPolicyGate"/> denies an autonomy edit.</summary>
    public const string AutonomyDenied = "AUTONOMY_DENIED";

    /// <summary><see cref="PolicyUpdateRecord.Field"/> written when a personality preset rebind succeeds.</summary>
    public const string PersonalityRebindField = "personality.rebind";

    /// <summary><see cref="PolicyUpdateRecord.Field"/> written when an autonomy tier rebind succeeds.</summary>
    public const string AutonomyRebindField = "autonomy";

    /// <summary>Same denial <see cref="DelegationOrchestrator.TryRebriefAgent"/> returns while a replay viewer is attached.</summary>
    public const string ReplayViewerPersonalityReason = "Replay viewer attached; rebrief is disabled.";

    /// <summary>Autonomy edits are refused while a replay viewer is attached.</summary>
    public const string ReplayViewerAutonomyReason = "Replay viewer attached; autonomy edit is disabled.";

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

    /// <summary>Whether a personality, autonomy, or rebrief edit is enabled, plus the gate message when it is not.</summary>
    public readonly record struct EditAvailability(bool Enabled, string? DeniedReason)
    {
        public static EditAvailability Allow() => new(true, null);

        public static EditAvailability Deny(string reason) => new(false, reason);
    }

    /// <summary>
    /// Hot personality edit. <see cref="LoopPolicyGate.CanEditPersonality"/> is authoritative.
    /// Under <see cref="PersonalityEditPolicy.TieredRebrief"/> at Semi-Autonomous or higher the gate denies;
    /// a paused sim clock unlocks that denial when <see cref="LoopPolicyGate.CanRebriefAgent"/> also allows.
    /// <see cref="PersonalityEditPolicy.PlanningOnly"/> stays locked after Begin Execution, including while paused.
    /// The separate Rebrief Agent command is the other sanctioned path and is not implied here.
    /// </summary>
    public static EditAvailability PersonalityEditAvailability(
        ScenarioPolicyProfile? policy,
        SimulationPhase phase,
        AutonomyLevel autonomy,
        bool simPaused)
    {
        var personality = LoopPolicyGate.CanEditPersonality(policy, phase, autonomy);
        var rebrief = LoopPolicyGate.CanRebriefAgent(policy, phase);
        if (personality.Allowed)
        {
            return EditAvailability.Allow();
        }

        var pauseUnlocksTier = simPaused
            && policy?.PersonalityEditPolicy == PersonalityEditPolicy.TieredRebrief
            && autonomy >= AutonomyLevel.SemiAutonomous
            && rebrief.Allowed;
        return pauseUnlocksTier
            ? EditAvailability.Allow()
            : EditAvailability.Deny(personality.DenialReason ?? rebrief.DenialReason ?? "Personality edit denied.");
    }

    /// <summary>Autonomy tier edit. <see cref="LoopPolicyGate.CanEditAutonomy"/> is authoritative.</summary>
    public static EditAvailability AutonomyEditAvailability(ScenarioPolicyProfile? policy, SimulationPhase phase)
    {
        var verdict = LoopPolicyGate.CanEditAutonomy(policy, phase);
        return verdict.Allowed
            ? EditAvailability.Allow()
            : EditAvailability.Deny(verdict.DenialReason ?? "Autonomy edit denied.");
    }

    /// <summary>Rebrief Agent action. <see cref="LoopPolicyGate.CanRebriefAgent"/> is authoritative.</summary>
    public static EditAvailability RebriefAvailability(ScenarioPolicyProfile? policy, SimulationPhase phase)
    {
        var verdict = LoopPolicyGate.CanRebriefAgent(policy, phase);
        return verdict.Allowed
            ? EditAvailability.Allow()
            : EditAvailability.Deny(verdict.DenialReason ?? "Rebrief denied.");
    }

    /// <summary>Denial text when the commanded target has no active agent controller.</summary>
    public static string NoAgentReason(string targetId) =>
        $"{targetId} has no active agent controller to rebrief.";

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

    /// <summary>
    /// Set the selected unit's agent to <paramref name="presetName"/>. Checks
    /// <see cref="LoopPolicyGate.CanEditPersonality"/> and <see cref="LoopPolicyGate.CanRebriefAgent"/>
    /// and returns the gate denial message. planningOnly after Begin Execution denies. tieredRebrief at
    /// Semi-Autonomous or higher denies unless the sim clock is paused (Rebrief Agent stays a separate command).
    /// A successful edit rebinds the preset through the orchestrator and appends <see cref="PersonalityRebindField"/>.
    /// </summary>
    public PlayEntryResult TrySetPersonality(TargetId targetId, string presetName, double simTime)
    {
        var resolved = ResolveAgent(targetId);
        if (resolved.Agent == null)
        {
            return resolved.Failure ?? PlayEntryResult.Fail(PersonalityDenied, "Personality edit denied.");
        }

        var agent = resolved.Agent;

        var preset = FindPreset(presetName);
        if (preset == null)
        {
            return UnknownPreset(presetName);
        }

        var orchestrator = _session.Bridge!.Orchestrator;
        var availability = PersonalityEditAvailability(
            orchestrator.ScenarioPolicy,
            orchestrator.Phase,
            agent.Autonomy,
            IsSimPaused());
        if (!availability.Enabled)
        {
            return PlayEntryResult.Fail(PersonalityDenied, availability.DeniedReason ?? "Personality edit denied.");
        }

        if (orchestrator.AttachReplayViewer)
        {
            return PlayEntryResult.Fail(PersonalityDenied, ReplayViewerPersonalityReason);
        }

        var previous = agent.PersonalitySlug ?? "custom";
        if (LoopPolicyGate.CanEditPersonality(orchestrator.ScenarioPolicy, orchestrator.Phase, agent.Autonomy).Allowed)
        {
            var rebound = orchestrator.TryRebindAgentTraits(agent, preset.Traits);
            if (!rebound.Allowed)
            {
                return PlayEntryResult.Fail(PersonalityDenied, rebound.DenialReason ?? "Personality edit denied.");
            }
        }

        var applied = orchestrator.TryRebriefAgent(agent, preset, simTime);
        if (!applied.Allowed)
        {
            return PlayEntryResult.Fail(PersonalityDenied, applied.DenialReason ?? "Personality edit denied.");
        }

        LogRebind(orchestrator, agent, simTime, PersonalityRebindField, previous, preset.Name);
        return PlayEntryResult.Ok($"Agent {agent.Id.Value} personality rebound {previous} → {preset.Name}.");
    }

    /// <summary>
    /// Set the selected unit's autonomy tier. Checks <see cref="LoopPolicyGate.CanEditAutonomy"/> and returns
    /// the gate denial message. There is no orchestrator autonomy writer; the façade sets
    /// <see cref="AgentController.Autonomy"/> and appends <see cref="AutonomyRebindField"/> on change.
    /// </summary>
    public PlayEntryResult TrySetAutonomy(TargetId targetId, AutonomyLevel autonomy, double simTime)
    {
        var resolved = ResolveAgent(targetId);
        if (resolved.Agent == null)
        {
            return resolved.Failure ?? PlayEntryResult.Fail(AutonomyDenied, "Autonomy edit denied.");
        }

        var agent = resolved.Agent;

        var orchestrator = _session.Bridge!.Orchestrator;
        var availability = AutonomyEditAvailability(orchestrator.ScenarioPolicy, orchestrator.Phase);
        if (!availability.Enabled)
        {
            return PlayEntryResult.Fail(AutonomyDenied, availability.DeniedReason ?? "Autonomy edit denied.");
        }

        if (orchestrator.AttachReplayViewer)
        {
            return PlayEntryResult.Fail(AutonomyDenied, ReplayViewerAutonomyReason);
        }

        if (agent.Autonomy == autonomy)
        {
            return PlayEntryResult.Ok($"Agent {agent.Id.Value} autonomy already {autonomy}.");
        }

        var previous = agent.Autonomy;
        agent.Autonomy = autonomy;
        LogRebind(orchestrator, agent, simTime, AutonomyRebindField, previous.ToString(), autonomy.ToString());
        return PlayEntryResult.Ok($"Agent {agent.Id.Value} autonomy rebound {previous} → {autonomy}.");
    }

    private readonly record struct AgentResolution(AgentController? Agent, PlayEntryResult? Failure);

    private AgentResolution ResolveAgent(TargetId targetId)
    {
        var blocked = RequireExecuting();
        if (blocked != null)
        {
            return new AgentResolution(null, blocked);
        }

        var commanded = FindCommanded(targetId);
        if (commanded == null)
        {
            return new AgentResolution(null, NotCommanded(targetId));
        }

        if (commanded.Target.Slot.Active is not AgentController active)
        {
            return new AgentResolution(
                null,
                PlayEntryResult.Fail(PlayDelegationErrorCodes.NoAgent, NoAgentReason(targetId.Value)));
        }

        return new AgentResolution(active, null);
    }

    private bool IsSimPaused() => _session.Bridge?.Session?.IsSimPaused == true;

    private static void LogRebind(
        DelegationOrchestrator orchestrator,
        AgentController agent,
        double simTime,
        string field,
        string previous,
        string next)
    {
        var simTick = (ulong)Math.Max(0, (long)simTime);
        orchestrator.DecisionLog.AppendPolicyUpdate(new PolicyUpdateRecord(
            0,
            simTime,
            simTick,
            agent.PolicySnapshotId,
            field,
            previous,
            next));
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
