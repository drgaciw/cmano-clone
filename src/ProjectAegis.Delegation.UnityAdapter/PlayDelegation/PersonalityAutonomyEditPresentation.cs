namespace ProjectAegis.Delegation.UnityAdapter.PlayDelegation;

using Controllers;
using Core;
using Orchestration;
using Traits;
using PlayEntry;

/// <summary>One personality preset or autonomy tier the player can apply, with the gate reason when it is disabled.</summary>
public sealed record PersonalityAutonomyOption(string Id, string Label, bool Enabled, string? DeniedReason);

/// <summary>Personality and autonomy edit state for one commanded unit. Actions go through <see cref="PlayDelegationCommands"/>.</summary>
public sealed record PersonalityAutonomyUnitEdit(
    string TargetId,
    string? AgentId,
    string? PersonalitySlug,
    AutonomyLevel? Autonomy,
    bool PersonalityEditEnabled,
    string? PersonalityDeniedReason,
    bool AutonomyEditEnabled,
    string? AutonomyDeniedReason,
    bool RebriefEnabled,
    string? RebriefDeniedReason,
    IReadOnlyList<PersonalityAutonomyOption> PersonalityOptions,
    IReadOnlyList<PersonalityAutonomyOption> AutonomyOptions);

/// <summary>
/// Headless personality / autonomy edit panel (DRG-250). Read-only projection of commanded targets and
/// <see cref="LoopPolicyGate"/> verdicts, including the paused-sim exception for tieredRebrief at Semi-Autonomous+.
/// Unity panel wiring is follow-up UI work. Does not mutate controllers or the order log (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public sealed record PersonalityAutonomyEditPresentation(
    bool IsAvailable,
    string? UnavailableReason,
    bool SimPaused,
    IReadOnlyList<PersonalityAutonomyOption> PersonalityOptions,
    IReadOnlyList<PersonalityAutonomyOption> AutonomyOptions,
    IReadOnlyList<PersonalityAutonomyUnitEdit> Units)
{
    public const string NoCommandedUnitReason = "No commanded unit.";

    public const string SelectUnitReason = "Select a commanded unit.";

    private static readonly AutonomyLevel[] Tiers =
    {
        AutonomyLevel.Manual,
        AutonomyLevel.Assisted,
        AutonomyLevel.SemiAutonomous,
        AutonomyLevel.FullAutonomous,
    };

    public static PersonalityAutonomyEditPresentation Project(PlayEntrySession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var simPaused = session.Bridge?.Session?.IsSimPaused == true;
        if (session.Package == null || session.Bridge == null)
        {
            return Unavailable(PlayEntryErrorCodes.NoPackage, simPaused);
        }

        var orchestrator = session.Bridge.Orchestrator;
        if (orchestrator.Phase != SimulationPhase.Executing)
        {
            return Unavailable(PlayDelegationErrorCodes.NotExecuting, simPaused);
        }

        var commanded = session.CommandedTargets;
        var units = new PersonalityAutonomyUnitEdit[commanded.Count];
        for (var i = 0; i < commanded.Count; i++)
        {
            units[i] = ProjectUnit(commanded[i], orchestrator, simPaused);
        }

        var (personality, autonomy) = PanelOptions(units);
        return new PersonalityAutonomyEditPresentation(true, null, simPaused, personality, autonomy, units);
    }

    private static PersonalityAutonomyUnitEdit ProjectUnit(
        PlayCommandedTarget commanded,
        DelegationOrchestrator orchestrator,
        bool simPaused)
    {
        var agent = commanded.Target.Slot.Active as AgentController;
        var targetId = commanded.Target.Id.Value;
        var personality = PersonalityAvailability(orchestrator, agent, targetId, simPaused);
        var autonomy = AutonomyAvailability(orchestrator, agent, targetId);
        var rebrief = RebriefAvailability(orchestrator, agent, targetId);
        return new PersonalityAutonomyUnitEdit(
            targetId,
            agent?.Id.Value,
            agent?.PersonalitySlug,
            agent?.Autonomy,
            personality.Enabled,
            personality.DeniedReason,
            autonomy.Enabled,
            autonomy.DeniedReason,
            rebrief.Enabled,
            rebrief.DeniedReason,
            BuildPersonalityOptions(personality),
            BuildAutonomyOptions(autonomy));
    }

    private static PlayDelegationCommands.EditAvailability PersonalityAvailability(
        DelegationOrchestrator orchestrator,
        AgentController? agent,
        string targetId,
        bool simPaused)
    {
        if (agent == null)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.NoAgentReason(targetId));
        }

        if (orchestrator.AttachReplayViewer)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.ReplayViewerPersonalityReason);
        }

        return PlayDelegationCommands.PersonalityEditAvailability(
            orchestrator.ScenarioPolicy,
            orchestrator.Phase,
            agent.Autonomy,
            simPaused);
    }

    private static PlayDelegationCommands.EditAvailability AutonomyAvailability(
        DelegationOrchestrator orchestrator,
        AgentController? agent,
        string targetId)
    {
        if (agent == null)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.NoAgentReason(targetId));
        }

        if (orchestrator.AttachReplayViewer)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.ReplayViewerAutonomyReason);
        }

        return PlayDelegationCommands.AutonomyEditAvailability(orchestrator.ScenarioPolicy, orchestrator.Phase);
    }

    private static PlayDelegationCommands.EditAvailability RebriefAvailability(
        DelegationOrchestrator orchestrator,
        AgentController? agent,
        string targetId)
    {
        if (agent == null)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.NoAgentReason(targetId));
        }

        if (orchestrator.AttachReplayViewer)
        {
            return PlayDelegationCommands.EditAvailability.Deny(PlayDelegationCommands.ReplayViewerPersonalityReason);
        }

        return PlayDelegationCommands.RebriefAvailability(orchestrator.ScenarioPolicy, orchestrator.Phase);
    }

    private static (IReadOnlyList<PersonalityAutonomyOption> Personality, IReadOnlyList<PersonalityAutonomyOption> Autonomy) PanelOptions(
        IReadOnlyList<PersonalityAutonomyUnitEdit> units)
    {
        if (units.Count == 1)
        {
            return (units[0].PersonalityOptions, units[0].AutonomyOptions);
        }

        var reason = units.Count == 0 ? NoCommandedUnitReason : SelectUnitReason;
        var denied = PlayDelegationCommands.EditAvailability.Deny(reason);
        return (BuildPersonalityOptions(denied), BuildAutonomyOptions(denied));
    }

    private static PersonalityAutonomyEditPresentation Unavailable(string reason, bool simPaused)
    {
        var denied = PlayDelegationCommands.EditAvailability.Deny(reason);
        return new PersonalityAutonomyEditPresentation(
            false,
            reason,
            simPaused,
            BuildPersonalityOptions(denied),
            BuildAutonomyOptions(denied),
            Array.Empty<PersonalityAutonomyUnitEdit>());
    }

    private static PersonalityAutonomyOption[] BuildPersonalityOptions(PlayDelegationCommands.EditAvailability availability)
    {
        var catalog = PersonalityCatalog.All;
        var options = new PersonalityAutonomyOption[catalog.Count];
        for (var i = 0; i < catalog.Count; i++)
        {
            var name = catalog[i].Name;
            options[i] = new PersonalityAutonomyOption(name, name, availability.Enabled, availability.DeniedReason);
        }

        return options;
    }

    private static PersonalityAutonomyOption[] BuildAutonomyOptions(PlayDelegationCommands.EditAvailability availability)
    {
        var options = new PersonalityAutonomyOption[Tiers.Length];
        for (var i = 0; i < Tiers.Length; i++)
        {
            var label = Tiers[i].ToString();
            options[i] = new PersonalityAutonomyOption(label, label, availability.Enabled, availability.DeniedReason);
        }

        return options;
    }
}
