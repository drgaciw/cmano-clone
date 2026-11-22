namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;

[TestFixture]
public sealed class C2PlayerCommandBridgeControlTests
{
    [Test]
    public void TryTakeControl_and_TryReturnToAgent_enforce_single_controller_and_group_detach_rejoin()
    {
        var bridge = new DelegationBridge(5, mvpEngagement: false);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "tg1");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.LinkGroupMember(group.TargetId, unit.TargetId);
        bridge.Registry.MarkFriendlyMeshMember(unit.TargetId);

        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("ga"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(agent, group.Target, EffectivePolicy.DefaultFree);

        Assert.That(C2PlayerCommandBridge.TryTakeControl(bridge, new EntityKey(1), 1.0, out var takeReason), Is.True, takeReason);
        Assert.That(unit.Target.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(((UnitTarget)unit.Target).IsDetachedFromGroup, Is.True);

        // Returning control on a detached group member must succeed via bridge facade and rejoin the group.
        Assert.That(C2PlayerCommandBridge.TryReturnToAgent(bridge, new EntityKey(1), 2.0, out var retReason), Is.True, retReason);
        Assert.That(((UnitTarget)unit.Target).IsDetachedFromGroup, Is.False);
        Assert.That(unit.Target.Slot.Active, Is.Null);
        Assert.That(((GroupTarget)group.Target).Members, Does.Contain(unit.TargetId));
        Assert.That(bridge.Orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryPauseAgentActions_and_TryResumeAgentActions_gate_execution_and_reject_duplicates()
    {
        var bridge = new DelegationBridge(7, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(agent, unit.Target, EffectivePolicy.DefaultFree);

        Assert.That(C2PlayerCommandBridge.TryPauseAgentActions(bridge, new EntityKey(1), 1.0, out var r1), Is.True, r1);
        Assert.That(agent.IsActionsPaused, Is.True);

        Assert.That(C2PlayerCommandBridge.TryPauseAgentActions(bridge, new EntityKey(1), 2.0, out var dupPause), Is.False);
        Assert.That(dupPause, Is.EqualTo(C2PlayerCommandBridge.ReasonAlreadyPaused));

        Assert.That(C2PlayerCommandBridge.TryResumeAgentActions(bridge, new EntityKey(1), 3.0, out var r2), Is.True, r2);
        Assert.That(agent.IsActionsPaused, Is.False);

        Assert.That(C2PlayerCommandBridge.TryResumeAgentActions(bridge, new EntityKey(1), 4.0, out var dupResume), Is.False);
        Assert.That(dupResume, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPaused));
        Assert.That(bridge.Orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(2));
    }

    [Test]
    public void Control_commands_refuse_opposing_ai_and_AttachReplayViewer_with_zero_log_writes()
    {
        var bridge = new DelegationBridge(9, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        var fa = bridge.Orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        var oa = bridge.Orchestrator.CreateAgent(new AgentId("a2"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(fa, friendly.Target, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.AssignAgentToTarget(oa, opposing.Target, EffectivePolicy.DefaultFree);

        Assert.That(C2PlayerCommandBridge.TryTakeControl(bridge, new EntityKey(2), 1.0, out var oppTake), Is.False);
        Assert.That(oppTake, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPlayerControlled));
        Assert.That(C2PlayerCommandBridge.TryPauseAgentActions(bridge, new EntityKey(2), 1.0, out var oppPause), Is.False);
        Assert.That(oppPause, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPlayerControlled));

        bridge.AttachReplayViewer = true;
        Assert.That(C2PlayerCommandBridge.TryTakeControl(bridge, new EntityKey(1), 2.0, out var repTake), Is.False);
        Assert.That(repTake, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(C2PlayerCommandBridge.TryPauseAgentActions(bridge, new EntityKey(1), 2.0, out var repPause), Is.False);
        Assert.That(repPause, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(bridge.Orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }
}
