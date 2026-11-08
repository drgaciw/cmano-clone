namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using Core;
using Traits;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>S124-05 / S124-06 Assign Agent + Rebrief panel presentation model (read-only projection).</summary>
[TestFixture]
public sealed class AssignAgentPanelPresentationTests
{
    [Test]
    public void Panel_is_unavailable_before_begin()
    {
        var panel = AssignAgentPanelPresentation.Project(new PlayEntrySession());

        Assert.That(panel.IsAvailable, Is.False);
        Assert.That(panel.UnavailableReason, Is.EqualTo(PlayEntryErrorCodes.NoPackage));
        Assert.That(panel.Rows, Is.Empty);
    }

    [Test]
    public void Panel_lists_commanded_human_target_as_assignable_with_presets()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var panel = AssignAgentPanelPresentation.Project(session);

        Assert.That(panel.IsAvailable, Is.True);
        Assert.That(panel.Presets, Is.EqualTo(PersonalityCatalog.All.Select(p => p.Name)));
        var row = panel.Rows.Single();
        Assert.That(row.TargetId, Is.EqualTo(friendly.Id.Value));
        Assert.That(row.Side, Is.EqualTo(PlaySide.Friendly));
        Assert.That(row.ControllerKind, Is.EqualTo("Human"));
        Assert.That(row.AgentId, Is.Null);
        Assert.That(row.CanAssign, Is.True);
        Assert.That(row.CanRebrief, Is.False);
        Assert.That(row.RebriefBlockedReason, Is.EqualTo(PlayDelegationErrorCodes.NoAgent));
    }

    [Test]
    public void Panel_reflects_assigned_agent_and_rebrief_after_commands()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        Assume.That(commands.TryRebriefAgent(friendly.Id, "Cautious", 1.0).Succeeded, Is.True);

        var row = AssignAgentPanelPresentation.Project(session).Rows.Single();

        Assert.That(row.ControllerKind, Is.EqualTo("Agent"));
        Assert.That(row.AgentId, Is.EqualTo(PlayDelegationCommands.AgentIdFor(friendly.Id).Value));
        Assert.That(row.PersonalitySlug, Is.EqualTo("Cautious"));
        Assert.That(row.Autonomy, Is.EqualTo(AutonomyLevel.SemiAutonomous));
        Assert.That(row.CanAssign, Is.False);
        Assert.That(row.CanRebrief, Is.True);
        Assert.That(row.RebriefBlockedReason, Is.Null);
    }

    [Test]
    public void Panel_surfaces_policy_rebrief_block_reason_under_planning_only()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: PersonalityEditPolicy.PlanningOnly);
        Assume.That(
            new PlayDelegationCommands(session).TryAssignAgent(friendly.Id, "Aggressive", AutonomyLevel.SemiAutonomous, 0).Succeeded,
            Is.True);

        var row = AssignAgentPanelPresentation.Project(session).Rows.Single();

        Assert.That(row.CanRebrief, Is.False);
        Assert.That(row.RebriefBlockedReason, Is.EqualTo("Personality locked after Begin Execution."));
    }

    [Test]
    public void Projection_does_not_mutate_controller_state_or_log()
    {
        var (session, friendly, _) = PlayDelegationCommandsTests.BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var log = session.Bridge!.Orchestrator.DecisionLog;
        var entriesBefore = log.ChronologicalEntries().Count;
        var active = friendly.Slot.Active;

        _ = AssignAgentPanelPresentation.Project(session);
        _ = AssignAgentPanelPresentation.Project(session);

        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(entriesBefore));
        Assert.That(friendly.Slot.Active, Is.SameAs(active));
    }
}
