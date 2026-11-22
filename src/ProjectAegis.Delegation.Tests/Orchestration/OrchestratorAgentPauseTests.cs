namespace ProjectAegis.Delegation.Tests.Orchestration;

using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;

[TestFixture]
public sealed class OrchestratorAgentPauseTests
{
    [Test]
    public void TryPauseAgentActions_suspends_intents_without_clearing_tasking_or_replacing_single_active_controller()
    {
        var orchestrator = new DelegationOrchestrator(7);
        var u1 = new UnitTarget(new TargetId("u1"));
        var u2 = new UnitTarget(new TargetId("u2"));
        orchestrator.Register(u1);
        orchestrator.Register(u2);

        var preset = PersonalityCatalog.All[0];
        var agent1 = orchestrator.CreateAgentFromPreset(new AgentId("a1"), preset, AutonomyLevel.FullAutonomous);
        var agent2 = orchestrator.CreateAgentFromPreset(new AgentId("a2"), preset, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent1, u1, EffectivePolicy.DefaultFree);
        orchestrator.AssignAgentToTarget(agent2, u2, EffectivePolicy.DefaultFree);
        orchestrator.BeginExecution(simTime: 0, simTick: 0);

        var snapshotBefore = agent1.PolicySnapshotId;
        var effectiveBefore = agent1.EffectivePolicy;
        var traitsBefore = agent1.Traits;
        var autonomyBefore = agent1.Autonomy;

        Assert.That(orchestrator.TryPauseAgentActions(u1, simTime: 1.0), Is.True);
        Assert.That(agent1.IsActionsPaused, Is.True);
        Assert.That(u1.Slot.Active, Is.SameAs(agent1));
        Assert.That(u1.Slot.SuspendedAgent, Is.Null);

        var state = new ObservedState(1.0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);
        orchestrator.Tick(state);
        orchestrator.Tick(state with { SimTime = 2.0 });

        Assert.That(orchestrator.DecisionLog.Records.Any(r => r.TargetId == u1.Id), Is.False,
            "Paused agent must not emit decisions.");
        Assert.That(orchestrator.ExecutedOrders.Any(o => o.Target == u1.Id), Is.False,
            "Paused agent must not emit orders.");
        Assert.That(orchestrator.DecisionLog.Records.Any(r => r.TargetId == u2.Id), Is.True,
            "Unpaused sibling agent must continue ticking normally.");

        Assert.That(agent1.PolicySnapshotId, Is.EqualTo(snapshotBefore));
        Assert.That(agent1.EffectivePolicy, Is.EqualTo(effectiveBefore));
        Assert.That(agent1.Traits, Is.EqualTo(traitsBefore));
        Assert.That(agent1.Autonomy, Is.EqualTo(autonomyBefore));
    }

    [Test]
    public void TryResumeAgentActions_restores_agent_intents_and_logs_controller_change()
    {
        var orchestrator = new DelegationOrchestrator(11);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var agent = orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        orchestrator.BeginExecution();

        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 1.0), Is.True);
        Assert.That(orchestrator.TryResumeAgentActions(unit, simTime: 2.0), Is.True);
        Assert.That(agent.IsActionsPaused, Is.False);

        var state = new ObservedState(3.0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);
        orchestrator.Tick(state);

        Assert.That(orchestrator.DecisionLog.Records.Any(r => r.TargetId == unit.Id), Is.True);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(2));
        Assert.That((orchestrator.DecisionLog.ControllerChanges[0].PreviousKind, orchestrator.DecisionLog.ControllerChanges[0].NewKind),
            Is.EqualTo(("Agent", "AgentPaused")));
        Assert.That((orchestrator.DecisionLog.ControllerChanges[1].PreviousKind, orchestrator.DecisionLog.ControllerChanges[1].NewKind),
            Is.EqualTo(("AgentPaused", "Agent")));
    }

    [Test]
    public void Pause_and_resume_reject_duplicate_calls_with_zero_duplicate_log_entries()
    {
        var orchestrator = new DelegationOrchestrator(13);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var agent = orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);

        Assert.That(orchestrator.TryResumeAgentActions(unit, simTime: 1.0), Is.False);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);

        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 2.0), Is.True);
        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 3.0), Is.False);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(1));

        Assert.That(orchestrator.TryResumeAgentActions(unit, simTime: 4.0), Is.True);
        Assert.That(orchestrator.TryResumeAgentActions(unit, simTime: 5.0), Is.False);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(2));
    }

    [Test]
    public void Pause_and_resume_return_false_and_log_nothing_when_AttachReplayViewer_enabled()
    {
        var orchestrator = new DelegationOrchestrator(17);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var agent = orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);

        orchestrator.AttachReplayViewer = true;
        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 1.0), Is.False);
        Assert.That(agent.IsActionsPaused, Is.False);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);

        orchestrator.AttachReplayViewer = false;
        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 2.0), Is.True);

        orchestrator.AttachReplayViewer = true;
        Assert.That(orchestrator.TryResumeAgentActions(unit, simTime: 3.0), Is.False);
        Assert.That(agent.IsActionsPaused, Is.True);
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(1));
    }

    [Test]
    public void PauseAgentActions_is_distinct_from_human_direct_control_and_sim_pause()
    {
        var orchestrator = new DelegationOrchestrator(19);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var agent = orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);

        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 1.0), Is.True);
        Assert.That(unit.Slot.Active, Is.Not.InstanceOf<HumanController>());
        Assert.That(unit.Slot.SuspendedAgent, Is.Null);
        Assert.That(orchestrator.TryReleaseDirectControl(unit, simTime: 2.0), Is.False);
    }

    [Test]
    public void TakeControl_while_paused_suspends_agent_and_ReturnToAgent_preserves_paused_state()
    {
        var orchestrator = new DelegationOrchestrator(23);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var agent = orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);

        Assert.That(orchestrator.TryPauseAgentActions(unit, simTime: 1.0), Is.True);
        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 2.0), Is.True);
        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(agent));

        Assert.That(orchestrator.TryReleaseDirectControl(unit, simTime: 3.0), Is.True);
        Assert.That(unit.Slot.Active, Is.SameAs(agent));
        Assert.That(agent.IsActionsPaused, Is.True);
        Assert.That(orchestrator.DecisionLog.ControllerChanges.Last().NewKind, Is.EqualTo("AgentPaused"));
    }
}
