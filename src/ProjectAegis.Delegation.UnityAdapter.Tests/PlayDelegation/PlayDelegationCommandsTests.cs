namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using Controllers;
using Core;
using Orchestration;
using Targets;
using Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
using ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S124-05 W2-DEL-04 initial Assign Agent and S124-06 W2-DEL-02 rebrief success path through the
/// headless play-delegation command façade. Failed commands preserve controller state and the order log.
/// </summary>
[TestFixture]
public sealed class PlayDelegationCommandsTests
{
    private const string Aggressive = "Aggressive";
    private const string Cautious = "Cautious";

    [Test]
    public void Begin_captures_human_controlled_targets_as_commanded_for_mixed_friendly()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        Assert.That(session.CommandedTargets, Has.Count.EqualTo(1));
        Assert.That(session.CommandedTargets[0].Target, Is.SameAs(friendly));
        Assert.That(session.CommandedTargets[0].Side, Is.EqualTo(PlaySide.Friendly));
    }

    [Test]
    public void Begin_captures_opposing_targets_when_player_commands_opposing_side()
    {
        var (session, _, opposing) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Opposing);

        Assert.That(session.CommandedTargets.Select(c => c.Target), Is.EqualTo(new[] { opposing }));
        Assert.That(session.CommandedTargets[0].Side, Is.EqualTo(PlaySide.Opposing));
    }

    [Test]
    public void Agent_vs_agent_has_no_commanded_targets_and_assign_is_refused()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.AgentVsAgent, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        var before = friendly.Slot.Active;

        var result = commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, simTime: 0);

        Assert.That(session.CommandedTargets, Is.Empty);
        AssertFailure(result, PlayDelegationErrorCodes.TargetNotCommanded);
        Assert.That(friendly.Slot.Active, Is.SameAs(before));
    }

    [Test]
    public void Reset_clears_commanded_targets()
    {
        var (session, _, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        Assert.That(session.TryResetToPlanning().Succeeded, Is.True);

        Assert.That(session.CommandedTargets, Is.Empty);
    }

    [Test]
    public void Assign_agent_after_begin_installs_preset_agent_and_logs_controller_change()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);

        var result = commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, simTime: 2.0);

        Assert.That(result.Succeeded, Is.True, result.Message);
        var agent = friendly.Slot.Active as AgentController;
        Assert.That(agent, Is.Not.Null);
        Assert.That(agent!.Id, Is.EqualTo(PlayDelegationCommands.AgentIdFor(friendly.Id)));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.SemiAutonomous));
        var change = session.Bridge!.Orchestrator.DecisionLog.ControllerChanges.Single();
        Assert.That((change.PreviousKind, change.NewKind), Is.EqualTo(("Human", "Agent")));
        Assert.That(change.AgentId, Is.EqualTo(agent.Id));
    }

    [Test]
    public void Assign_agent_in_planning_is_refused_because_begin_configures_controllers()
    {
        var session = new PlayEntrySession();
        Assume.That(session.TryLoad(PlayEntrySessionTests.BalticEntry()).Succeeded, Is.True);
        var (friendly, _) = PlayEntrySessionTests.RegisterBalticForces(session.Bridge!);

        var result = new PlayDelegationCommands(session)
            .TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NotExecuting);
        Assert.That(friendly.Slot.Active, Is.Null);
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void Assign_without_package_is_refused()
    {
        var result = new PlayDelegationCommands(new PlayEntrySession())
            .TryAssignAgent(new TargetId("u1"), Aggressive, AutonomyLevel.SemiAutonomous, simTime: 0);

        AssertFailure(result, PlayEntryErrorCodes.NoPackage);
    }

    [Test]
    public void Assign_to_opposing_target_when_commanding_friendly_is_refused_without_mutation()
    {
        var (session, _, opposing) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var opposingAgent = opposing.Slot.Active;

        var result = new PlayDelegationCommands(session)
            .TryAssignAgent(opposing.Id, Aggressive, AutonomyLevel.SemiAutonomous, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.TargetNotCommanded);
        Assert.That(opposing.Slot.Active, Is.SameAs(opposingAgent));
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void Assign_with_unknown_preset_is_refused_without_mutation()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var human = friendly.Slot.Active;

        var result = new PlayDelegationCommands(session)
            .TryAssignAgent(friendly.Id, "NotAPreset", AutonomyLevel.SemiAutonomous, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.UnknownPreset);
        Assert.That(friendly.Slot.Active, Is.SameAs(human));
    }

    [Test]
    public void Second_assign_is_denied_by_the_orchestrator_and_preserves_the_first_agent()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        var first = friendly.Slot.Active;

        var result = commands.TryAssignAgent(friendly.Id, Cautious, AutonomyLevel.FullAutonomous, simTime: 1.0);

        AssertFailure(result, PlayDelegationErrorCodes.AssignDenied);
        Assert.That(result.Message, Does.Contain("already has an agent"));
        Assert.That(friendly.Slot.Active, Is.SameAs(first));
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(1));
    }

    [Test]
    public void Rebrief_success_under_tiered_rebrief_updates_agent_and_logs_reason()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = ProfileWith(PersonalityEditPolicy.TieredRebrief);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        var agent = (AgentController)friendly.Slot.Active!;
        Assert.That(
            session.Bridge.Orchestrator.TryRebindAgentTraits(agent, PresetNamed(Cautious).Traits).Allowed,
            Is.False,
            "Deny-only DEL-02 hot rebind still refuses at Semi-Autonomous under TieredRebrief.");

        var result = commands.TryRebriefAgent(friendly.Id, Cautious, simTime: 4.0);

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(result.Message, Does.Contain(Aggressive).And.Contain(Cautious));
        Assert.That(friendly.Slot.Active, Is.SameAs(agent));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Cautious));
        Assert.That(agent.Traits, Is.EqualTo(PresetNamed(Cautious).Traits));
        var update = session.Bridge.Orchestrator.DecisionLog.PolicyUpdates
            .Single(u => u.Field == DelegationOrchestrator.RebriefPolicyField);
        Assert.That((update.PreviousValue, update.NewValue), Is.EqualTo((Aggressive, Cautious)));
    }

    [Test]
    public void Rebrief_denied_under_planning_only_reports_reason_and_preserves_state()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = ProfileWith(PersonalityEditPolicy.PlanningOnly);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        var agent = (AgentController)friendly.Slot.Active!;
        var updatesBefore = session.Bridge.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TryRebriefAgent(friendly.Id, Cautious, simTime: 4.0);

        AssertFailure(result, PlayDelegationErrorCodes.RebriefDenied);
        Assert.That(result.Message, Does.Contain("Personality locked after Begin Execution."));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(agent.Traits, Is.EqualTo(PresetNamed(Aggressive).Traits));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updatesBefore));
    }

    [Test]
    public void Rebrief_of_human_controlled_target_is_refused()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var result = new PlayDelegationCommands(session).TryRebriefAgent(friendly.Id, Cautious, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NoAgent);
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
    }

    internal static (PlayEntrySession Session, ICommandableTarget Friendly, ICommandableTarget Opposing) BeginBaltic(
        SimulationModeKind mode,
        PlaySide side)
    {
        var session = new PlayEntrySession();
        Assert.That(session.TryLoad(PlayEntrySessionTests.BalticEntry()).Succeeded, Is.True);
        Assert.That(session.TrySelectMode(mode).Succeeded, Is.True);
        Assert.That(session.TrySelectSide(side).Succeeded, Is.True);
        var (friendly, opposing) = PlayEntrySessionTests.RegisterBalticForces(session.Bridge!);
        var begin = session.TryBeginExecution(new[] { friendly }, new[] { opposing });
        Assert.That(begin.Succeeded, Is.True, begin.Message);
        return (session, friendly, opposing);
    }

    private static PersonalityPreset PresetNamed(string name) =>
        PersonalityCatalog.All.Single(p => p.Name == name);

    private static ScenarioPolicyProfile ProfileWith(PersonalityEditPolicy editPolicy) =>
        new(EffectivePolicy.DefaultFree, personalityEditPolicy: editPolicy);

    private static void AssertFailure(PlayEntryResult result, string expectedCode)
    {
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expectedCode));
        Assert.That(result.Message, Is.Not.Empty);
    }
}
