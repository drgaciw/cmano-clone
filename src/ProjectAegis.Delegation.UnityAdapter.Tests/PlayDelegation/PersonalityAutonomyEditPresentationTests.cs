namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using Core;
using Traits;
using PlayEntry;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// DRG-250 headless personality / autonomy edit panel. Projection only: options plus the gate's enabled or denied reason.
/// </summary>
[TestFixture]
public sealed class PersonalityAutonomyEditPresentationTests
{
    [Test]
    public void Panel_is_unavailable_before_a_package_is_loaded()
    {
        var panel = PersonalityAutonomyEditPresentation.Project(new PlayEntrySession());

        Assert.That(panel.IsAvailable, Is.False);
        Assert.That(panel.UnavailableReason, Is.EqualTo(PlayEntryErrorCodes.NoPackage));
        Assert.That(panel.Units, Is.Empty);
        Assert.That(panel.SimPaused, Is.False);
        AssertDisabled(panel.PersonalityOptions, PlayEntryErrorCodes.NoPackage, PersonalityCatalog.All.Select(p => p.Name));
        AssertDisabled(panel.AutonomyOptions, PlayEntryErrorCodes.NoPackage, TierLabels());
    }

    [Test]
    public void Panel_is_unavailable_before_begin()
    {
        var session = new PlayEntrySession();
        Assume.That(session.TryLoad(PlayEntrySessionTests.BalticEntry()).Succeeded, Is.True);

        var panel = PersonalityAutonomyEditPresentation.Project(session);

        Assert.That(panel.IsAvailable, Is.False);
        Assert.That(panel.UnavailableReason, Is.EqualTo(PlayDelegationErrorCodes.NotExecuting));
        Assert.That(panel.Units, Is.Empty);
        AssertDisabled(panel.PersonalityOptions, PlayDelegationErrorCodes.NotExecuting, PersonalityCatalog.All.Select(p => p.Name));
    }

    [Test]
    public void Panel_with_no_commanded_unit_lists_disabled_options()
    {
        var (session, _, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.AgentVsAgent, PlaySide.Friendly);

        var panel = PersonalityAutonomyEditPresentation.Project(session);

        Assert.That(panel.IsAvailable, Is.True);
        Assert.That(panel.Units, Is.Empty);
        AssertDisabled(
            panel.PersonalityOptions,
            PersonalityAutonomyEditPresentation.NoCommandedUnitReason,
            PersonalityCatalog.All.Select(p => p.Name));
        AssertDisabled(panel.AutonomyOptions, PersonalityAutonomyEditPresentation.NoCommandedUnitReason, TierLabels());
    }

    [Test]
    public void Panel_disables_edits_until_the_unit_has_an_agent()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var panel = PersonalityAutonomyEditPresentation.Project(session);

        Assert.That(panel.IsAvailable, Is.True);
        var unit = panel.Units.Single();
        var reason = PlayDelegationCommands.NoAgentReason(friendly.Id.Value);
        Assert.That(unit.TargetId, Is.EqualTo(friendly.Id.Value));
        Assert.That(unit.AgentId, Is.Null);
        Assert.That(unit.PersonalityEditEnabled, Is.False);
        Assert.That(unit.AutonomyEditEnabled, Is.False);
        Assert.That(unit.RebriefEnabled, Is.False);
        Assert.That(unit.PersonalityDeniedReason, Is.EqualTo(reason));
        Assert.That(unit.AutonomyDeniedReason, Is.EqualTo(reason));
        Assert.That(unit.RebriefDeniedReason, Is.EqualTo(reason));
        AssertDisabled(unit.PersonalityOptions, reason, PersonalityCatalog.All.Select(p => p.Name));
        AssertDisabled(unit.AutonomyOptions, reason, TierLabels());
        Assert.That(panel.PersonalityOptions, Is.EqualTo(unit.PersonalityOptions));
    }

    [Test]
    public void Panel_enables_personality_and_autonomy_after_an_anytime_assign()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = Profile(PersonalityEditPolicy.Anytime);
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded,
            Is.True);

        var unit = PersonalityAutonomyEditPresentation.Project(session).Units.Single();

        Assert.That(unit.AgentId, Is.EqualTo(PlayDelegationCommands.AgentIdFor(friendly.Id).Value));
        Assert.That(unit.PersonalitySlug, Is.EqualTo("Aggressive"));
        Assert.That(unit.Autonomy, Is.EqualTo(AutonomyLevel.SemiAutonomous));
        Assert.That(unit.PersonalityEditEnabled, Is.True);
        Assert.That(unit.PersonalityDeniedReason, Is.Null);
        Assert.That(unit.AutonomyEditEnabled, Is.True);
        Assert.That(unit.AutonomyDeniedReason, Is.Null);
        Assert.That(unit.RebriefEnabled, Is.True);
        Assert.That(unit.RebriefDeniedReason, Is.Null);
        Assert.That(unit.PersonalityOptions.Select(o => o.Id), Is.EqualTo(PersonalityCatalog.All.Select(p => p.Name)));
        Assert.That(unit.AutonomyOptions.Select(o => o.Id), Is.EqualTo(TierLabels()));
        Assert.That(unit.PersonalityOptions.All(o => o.Enabled && o.DeniedReason == null), Is.True);
        Assert.That(unit.AutonomyOptions.All(o => o.Enabled && o.DeniedReason == null), Is.True);
    }

    [Test]
    public void Panel_surfaces_planning_only_lock_and_still_allows_autonomy()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = Profile(PersonalityEditPolicy.PlanningOnly);
        session.Bridge.Session!.PauseSim();
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded,
            Is.True);

        var panel = PersonalityAutonomyEditPresentation.Project(session);
        var unit = panel.Units.Single();

        Assert.That(panel.SimPaused, Is.True);
        Assert.That(unit.PersonalityEditEnabled, Is.False);
        Assert.That(unit.PersonalityDeniedReason, Is.EqualTo("Personality locked after Begin Execution."));
        Assert.That(unit.RebriefEnabled, Is.False);
        Assert.That(unit.RebriefDeniedReason, Is.EqualTo("Personality locked after Begin Execution."));
        Assert.That(unit.AutonomyEditEnabled, Is.True);
        Assert.That(unit.AutonomyDeniedReason, Is.Null);
        AssertDisabled(unit.PersonalityOptions, "Personality locked after Begin Execution.", PersonalityCatalog.All.Select(p => p.Name));
        Assert.That(unit.AutonomyOptions.All(o => o.Enabled), Is.True);
    }

    [Test]
    public void Panel_denies_tiered_semi_personality_until_pause_and_keeps_rebrief_enabled()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = Profile(PersonalityEditPolicy.TieredRebrief);
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded,
            Is.True);

        var running = PersonalityAutonomyEditPresentation.Project(session).Units.Single();

        Assert.That(running.PersonalityEditEnabled, Is.False);
        Assert.That(running.PersonalityDeniedReason, Is.EqualTo("Rebrief Agent required at Semi-Autonomous or higher."));
        Assert.That(running.RebriefEnabled, Is.True);
        Assert.That(running.RebriefDeniedReason, Is.Null);
        Assert.That(running.AutonomyEditEnabled, Is.True);
        AssertDisabled(
            running.PersonalityOptions,
            "Rebrief Agent required at Semi-Autonomous or higher.",
            PersonalityCatalog.All.Select(p => p.Name));

        session.Bridge.Session!.PauseSim();
        var paused = PersonalityAutonomyEditPresentation.Project(session).Units.Single();

        Assert.That(paused.PersonalityEditEnabled, Is.True);
        Assert.That(paused.PersonalityDeniedReason, Is.Null);
        Assert.That(paused.PersonalityOptions.All(o => o.Enabled && o.DeniedReason == null), Is.True);
        Assert.That(paused.RebriefEnabled, Is.True);
    }

    [Test]
    public void Panel_allows_tiered_assisted_personality_without_pause()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = Profile(PersonalityEditPolicy.TieredRebrief);
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Defensive", AutonomyLevel.Assisted, 0).Succeeded,
            Is.True);

        var unit = PersonalityAutonomyEditPresentation.Project(session).Units.Single();

        Assert.That(session.Bridge.Session!.IsSimPaused, Is.False);
        Assert.That(unit.PersonalitySlug, Is.EqualTo("Defensive"));
        Assert.That(unit.Autonomy, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(unit.PersonalityEditEnabled, Is.True);
        Assert.That(unit.PersonalityDeniedReason, Is.Null);
    }

    [Test]
    public void Panel_disables_edits_while_a_replay_viewer_is_attached()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded,
            Is.True);
        session.Bridge!.Orchestrator.AttachReplayViewer = true;

        var unit = PersonalityAutonomyEditPresentation.Project(session).Units.Single();

        Assert.That(unit.PersonalityEditEnabled, Is.False);
        Assert.That(unit.PersonalityDeniedReason, Is.EqualTo(PlayDelegationCommands.ReplayViewerPersonalityReason));
        Assert.That(unit.AutonomyEditEnabled, Is.False);
        Assert.That(unit.AutonomyDeniedReason, Is.EqualTo(PlayDelegationCommands.ReplayViewerAutonomyReason));
        Assert.That(unit.RebriefEnabled, Is.False);
        Assert.That(unit.RebriefDeniedReason, Is.EqualTo(PlayDelegationCommands.ReplayViewerPersonalityReason));
    }

    [Test]
    public void Projection_does_not_mutate_controller_state_or_log()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var log = session.Bridge!.Orchestrator.DecisionLog;
        var entriesBefore = log.ChronologicalEntries().Count;
        var active = friendly.Slot.Active;

        _ = PersonalityAutonomyEditPresentation.Project(session);
        _ = PersonalityAutonomyEditPresentation.Project(session);

        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(entriesBefore));
        Assert.That(friendly.Slot.Active, Is.SameAs(active));
    }

    private static ScenarioPolicyProfile Profile(PersonalityEditPolicy editPolicy) =>
        new(EffectivePolicy.DefaultFree, personalityEditPolicy: editPolicy);

    private static string[] TierLabels() =>
    [
        AutonomyLevel.Manual.ToString(),
        AutonomyLevel.Assisted.ToString(),
        AutonomyLevel.SemiAutonomous.ToString(),
        AutonomyLevel.FullAutonomous.ToString(),
    ];

    private static void AssertDisabled(
        IReadOnlyList<PersonalityAutonomyOption> options,
        string reason,
        IEnumerable<string> ids)
    {
        Assert.That(options.Select(o => o.Id), Is.EqualTo(ids));
        Assert.That(options, Is.Not.Empty);
        Assert.That(options.All(o => !o.Enabled && o.DeniedReason == reason && o.Label == o.Id), Is.True);
    }
}
