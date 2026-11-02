namespace ProjectAegis.Delegation.UnityAdapter.Tests.PlayDelegation;

using Controllers;
using Core;
using Orchestration;
using Targets;
using Traits;
using PlayEntry;
using ProjectAegis.Delegation.UnityAdapter.PlayDelegation;
using ProjectAegis.Delegation.UnityAdapter.PlayEntry;
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
    public void Assign_refuses_while_human_orders_are_pending()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        var human = (HumanController)friendly.Slot.Active!;
        human.Enqueue(new Order(new OrderId(1), friendly.Id, 0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 99);
        var commands = new PlayDelegationCommands(session);

        var result = commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, simTime: 2.0);

        AssertFailure(result, PlayDelegationErrorCodes.AssignDenied);
        Assert.That(result.Message, Does.Contain(DelegationOrchestrator.PendingHumanOrdersCause));
        Assert.That(friendly.Slot.Active, Is.SameAs(human));
        Assert.That(human.PendingOrderCount, Is.EqualTo(1));
        Assert.That(session.Bridge!.Orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void Rebrief_of_human_controlled_target_is_refused()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var result = new PlayDelegationCommands(session).TryRebriefAgent(friendly.Id, Cautious, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NoAgent);
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [TestCase(PersonalityEditPolicy.Anytime, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, false, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Planning, AutonomyLevel.FullAutonomous, false, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Executing, AutonomyLevel.Manual, false, false)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, true, false)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.Manual, false, true)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.Assisted, false, true)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.SemiAutonomous, false, false)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, false, false)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.SemiAutonomous, true, true)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, true, true)]
    public void Personality_edit_availability_matches_the_gate(
        PersonalityEditPolicy editPolicy,
        SimulationPhase phase,
        AutonomyLevel autonomy,
        bool simPaused,
        bool expectedEnabled)
    {
        var profile = ProfileWith(editPolicy);
        var availability = PlayDelegationCommands.PersonalityEditAvailability(profile, phase, autonomy, simPaused);
        var gate = LoopPolicyGate.CanEditPersonality(profile, phase, autonomy);

        Assert.That(availability.Enabled, Is.EqualTo(expectedEnabled));
        if (expectedEnabled)
        {
            Assert.That(availability.DeniedReason, Is.Null);
        }
        else
        {
            Assert.That(availability.DeniedReason, Is.EqualTo(gate.DenialReason));
        }
    }

    [Test]
    public void Autonomy_edit_stays_available_when_personality_is_planning_locked()
    {
        var profile = ProfileWith(PersonalityEditPolicy.PlanningOnly);
        var availability = PlayDelegationCommands.AutonomyEditAvailability(profile, SimulationPhase.Executing);

        Assert.That(availability.Enabled, Is.True);
        Assert.That(availability.DeniedReason, Is.Null);
        Assert.That(
            LoopPolicyGate.CanEditPersonality(profile, SimulationPhase.Executing, AutonomyLevel.SemiAutonomous).Allowed,
            Is.False);
    }

    [Test]
    public void Rebrief_stays_available_at_semi_when_hot_edit_is_denied()
    {
        var profile = ProfileWith(PersonalityEditPolicy.TieredRebrief);
        var rebrief = PlayDelegationCommands.RebriefAvailability(profile, SimulationPhase.Executing);
        var hot = PlayDelegationCommands.PersonalityEditAvailability(
            profile,
            SimulationPhase.Executing,
            AutonomyLevel.SemiAutonomous,
            simPaused: false);

        Assert.That(rebrief.Enabled, Is.True);
        Assert.That(hot.Enabled, Is.False);
        Assert.That(hot.DeniedReason, Is.EqualTo("Rebrief Agent required at Semi-Autonomous or higher."));
    }

    [Test]
    public void Set_personality_without_package_is_refused()
    {
        var result = new PlayDelegationCommands(new PlayEntrySession())
            .TrySetPersonality(new TargetId("u1"), Cautious, simTime: 0);

        AssertFailure(result, PlayEntryErrorCodes.NoPackage);
    }

    [Test]
    public void Set_personality_in_planning_is_refused()
    {
        var session = new PlayEntrySession();
        Assume.That(session.TryLoad(PlayEntrySessionTests.BalticEntry()).Succeeded, Is.True);
        var (friendly, _) = PlayEntrySessionTests.RegisterBalticForces(session.Bridge!);

        var result = new PlayDelegationCommands(session).TrySetPersonality(friendly.Id, Cautious, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NotExecuting);
    }

    [Test]
    public void Set_personality_on_uncommanded_target_is_refused()
    {
        var (session, friendly, opposing) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = ProfileWith(PersonalityEditPolicy.Anytime);
        var commands = new PlayDelegationCommands(session);
        Assume.That(commands.TryAssignAgent(friendly.Id, Aggressive, AutonomyLevel.SemiAutonomous, 0).Succeeded, Is.True);
        var opposingBefore = opposing.Slot.Active;

        var result = commands.TrySetPersonality(opposing.Id, Cautious, simTime: 1);

        AssertFailure(result, PlayDelegationErrorCodes.TargetNotCommanded);
        Assert.That(((AgentController)friendly.Slot.Active!).PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(opposing.Slot.Active, Is.SameAs(opposingBefore));
    }

    [Test]
    public void Set_personality_on_human_controller_is_refused()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var result = new PlayDelegationCommands(session).TrySetPersonality(friendly.Id, Cautious, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NoAgent);
        Assert.That(result.Message, Is.EqualTo(PlayDelegationCommands.NoAgentReason(friendly.Id.Value)));
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Set_personality_with_unknown_preset_is_refused_without_mutation()
    {
        var (session, friendly, agent, commands) = AssignPreset();
        var updates = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TrySetPersonality(friendly.Id, "NotAPreset", simTime: 1);

        AssertFailure(result, PlayDelegationErrorCodes.UnknownPreset);
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_personality_under_planning_only_after_begin_returns_the_gate_message()
    {
        var (session, friendly, agent, commands) = AssignPreset(PersonalityEditPolicy.PlanningOnly);
        Pause(session);
        var updates = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates.Count;
        var traits = agent.Traits;
        var autonomy = agent.Autonomy;

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 3);

        AssertFailure(result, PlayDelegationCommands.PersonalityDenied);
        Assert.That(result.Message, Is.EqualTo("Personality locked after Begin Execution."));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(agent.Traits, Is.EqualTo(traits));
        Assert.That(agent.Autonomy, Is.EqualTo(autonomy));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_personality_under_tiered_rebrief_at_semi_returns_the_gate_message()
    {
        var (session, friendly, agent, commands) = AssignPreset(PersonalityEditPolicy.TieredRebrief);
        var updates = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 3);

        AssertFailure(result, PlayDelegationCommands.PersonalityDenied);
        Assert.That(result.Message, Is.EqualTo("Rebrief Agent required at Semi-Autonomous or higher."));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(agent.Traits, Is.EqualTo(PresetNamed(Aggressive).Traits));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_personality_under_tiered_rebrief_at_full_autonomy_is_denied_until_pause()
    {
        var (session, friendly, agent, commands) = AssignPreset(
            PersonalityEditPolicy.TieredRebrief,
            AutonomyLevel.FullAutonomous);

        var denied = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 2);

        AssertFailure(denied, PlayDelegationCommands.PersonalityDenied);
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));

        Pause(session);
        var allowed = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 4);

        Assert.That(allowed.Succeeded, Is.True, allowed.Message);
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Cautious));
    }

    [Test]
    public void Set_personality_under_tiered_rebrief_at_assisted_succeeds_without_pause_and_logs_rebind()
    {
        var (session, friendly, agent, commands) = AssignPreset(
            PersonalityEditPolicy.TieredRebrief,
            AutonomyLevel.Assisted);
        Assert.That(session.Bridge!.Session!.IsSimPaused, Is.False);

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 5);

        AssertRebound(session, agent, result, Cautious);
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.Assisted));
    }

    [Test]
    public void Set_personality_when_paused_under_tiered_rebrief_at_semi_logs_rebind()
    {
        var (session, friendly, agent, commands) = AssignPreset(PersonalityEditPolicy.TieredRebrief);
        Pause(session);

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 6);

        AssertRebound(session, agent, result, Cautious);
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.SemiAutonomous));
    }

    [Test]
    public void Set_personality_anytime_at_full_autonomy_logs_rebind()
    {
        var (session, friendly, agent, commands) = AssignPreset(autonomy: AutonomyLevel.FullAutonomous);

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 2.5);

        AssertRebound(session, agent, result, Cautious);
    }

    [Test]
    public void Set_personality_same_preset_does_not_log_a_rebind()
    {
        var (session, friendly, agent, commands) = AssignPreset();
        var updates = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TrySetPersonality(friendly.Id, Aggressive, simTime: 1);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Message, Does.Contain("already briefed"));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_personality_same_preset_after_custom_trait_rebind_restores_preset_and_logs()
    {
        var (session, friendly, agent, commands) = AssignPreset();
        var customTraits = PresetNamed(Cautious).Traits;
        Assume.That(session.Bridge!.Orchestrator.TryRebindAgentTraits(agent, customTraits).Allowed, Is.True);
        Assume.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assume.That(agent.Traits, Is.EqualTo(customTraits));

        var result = commands.TrySetPersonality(friendly.Id, Aggressive, simTime: 3.0);

        AssertRebound(session, agent, result, Aggressive);
    }

    [Test]
    public void Set_personality_while_replay_viewer_attached_does_not_mutate()
    {
        var (session, friendly, agent, commands) = AssignPreset();
        session.Bridge!.Orchestrator.AttachReplayViewer = true;
        var updates = session.Bridge.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TrySetPersonality(friendly.Id, Cautious, simTime: 1);

        AssertFailure(result, PlayDelegationCommands.PersonalityDenied);
        Assert.That(result.Message, Is.EqualTo(PlayDelegationCommands.ReplayViewerPersonalityReason));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_autonomy_under_planning_only_logs_rebind_and_keeps_personality()
    {
        var (session, friendly, agent, commands) = AssignPreset(PersonalityEditPolicy.PlanningOnly);

        var result = commands.TrySetAutonomy(friendly.Id, AutonomyLevel.Manual, simTime: 8);

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(result.Message, Does.Contain("rebound").And.Contain("Manual"));
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.Manual));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive));
        var update = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates
            .Single(u => u.Field == PlayDelegationCommands.AutonomyRebindField);
        Assert.That((update.PreviousValue, update.NewValue), Is.EqualTo(("SemiAutonomous", "Manual")));
        Assert.That(update.SimTime, Is.EqualTo(8));
    }

    [Test]
    public void Set_autonomy_same_tier_does_not_log()
    {
        var (session, friendly, agent, commands) = AssignPreset(PersonalityEditPolicy.PlanningOnly);
        var updates = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates.Count;

        var result = commands.TrySetAutonomy(friendly.Id, agent.Autonomy, simTime: 1);

        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(session.Bridge.Orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updates));
    }

    [Test]
    public void Set_autonomy_on_human_controller_is_refused()
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);

        var result = new PlayDelegationCommands(session).TrySetAutonomy(friendly.Id, AutonomyLevel.Manual, simTime: 0);

        AssertFailure(result, PlayDelegationErrorCodes.NoAgent);
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Set_autonomy_while_replay_viewer_attached_does_not_mutate()
    {
        var (session, friendly, agent, commands) = AssignPreset();
        session.Bridge!.Orchestrator.AttachReplayViewer = true;

        var result = commands.TrySetAutonomy(friendly.Id, AutonomyLevel.Manual, simTime: 1);

        AssertFailure(result, PlayDelegationCommands.AutonomyDenied);
        Assert.That(result.Message, Is.EqualTo(PlayDelegationCommands.ReplayViewerAutonomyReason));
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.SemiAutonomous));
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

    private static (PlayEntrySession Session, ICommandableTarget Friendly, AgentController Agent, PlayDelegationCommands Commands) AssignPreset(
        PersonalityEditPolicy policy = PersonalityEditPolicy.Anytime,
        AutonomyLevel autonomy = AutonomyLevel.SemiAutonomous)
    {
        var (session, friendly, _) = BeginBaltic(SimulationModeKind.Mixed, PlaySide.Friendly);
        session.Bridge!.Orchestrator.ScenarioPolicy = ProfileWith(policy);
        var commands = new PlayDelegationCommands(session);
        var assigned = commands.TryAssignAgent(friendly.Id, Aggressive, autonomy, simTime: 0);
        Assert.That(assigned.Succeeded, Is.True, assigned.Message);
        var agent = friendly.Slot.Active as AgentController;
        Assert.That(agent, Is.Not.Null);
        return (session, friendly, agent!, commands);
    }

    private static void Pause(PlayEntrySession session)
    {
        Assert.That(session.Bridge!.Session, Is.Not.Null);
        session.Bridge.Session!.PauseSim();
        Assert.That(session.Bridge.Session.IsSimPaused, Is.True);
    }

    private static void AssertRebound(PlayEntrySession session, AgentController agent, PlayEntryResult result, string presetName)
    {
        Assert.That(result.Succeeded, Is.True, result.Message);
        Assert.That(result.Message, Does.Contain("rebound").And.Contain(presetName));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(presetName));
        Assert.That(agent.Traits, Is.EqualTo(PresetNamed(presetName).Traits));
        var update = session.Bridge!.Orchestrator.DecisionLog.PolicyUpdates
            .Single(u => u.Field == PlayDelegationCommands.PersonalityRebindField);
        Assert.That((update.PreviousValue, update.NewValue), Is.EqualTo((Aggressive, presetName)));
        var rebrief = session.Bridge.Orchestrator.DecisionLog.PolicyUpdates
            .Single(u => u.Field == DelegationOrchestrator.RebriefPolicyField);
        Assert.That((rebrief.PreviousValue, rebrief.NewValue), Is.EqualTo((Aggressive, presetName)));
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
