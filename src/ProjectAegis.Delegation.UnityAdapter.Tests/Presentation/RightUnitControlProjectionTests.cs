using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class RightUnitControlProjectionTests
{
    [Test]
    public void Project_reflects_human_agent_paused_detached_opposing_none_and_replay_viewer()
    {
        var empty = RightUnitControlProjection.Project(null, null);
        Assert.That(empty.IsDisabled, Is.True);
        Assert.That(empty.CanTakeControl, Is.False);
        Assert.That(empty.CanReturnToAgent, Is.False);
        Assert.That(empty.CanPauseAgent, Is.False);
        Assert.That(empty.CanResumeAgent, Is.False);

        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "tg1");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var hostile = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.Registry.LinkGroupMember(group.TargetId, unit.TargetId);
        bridge.Registry.MarkFriendlyMeshMember(unit.TargetId);

        var agent = bridge.Orchestrator.CreateAgent(new AgentId("a1"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(agent, group.Target, EffectivePolicy.DefaultFree);
        var hostileAgent = bridge.Orchestrator.CreateAgent(new AgentId("ha"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(hostileAgent, hostile.Target, EffectivePolicy.DefaultFree);

        // Undetached group member: TakeControl is allowed via group detach, PauseAgent is only for own agent.
        var groupMemberState = RightUnitControlProjection.Project(bridge, "u1");
        Assert.That(groupMemberState.IsDisabled, Is.False);
        Assert.That(groupMemberState.CanTakeControl, Is.True);
        Assert.That(groupMemberState.CanReturnToAgent, Is.False);

        // Standalone agent -> Active -> Paused -> Human -> ReplayViewer
        var standalone = bridge.Registry.RegisterUnit(new EntityKey(3), "u3");
        var sa = bridge.Orchestrator.CreateAgent(new AgentId("a3"), PersonalityCatalog.All[0].Traits, AutonomyLevel.FullAutonomous);
        bridge.Orchestrator.AssignAgentToTarget(sa, standalone.Target, EffectivePolicy.DefaultFree);

        var activeAgent = RightUnitControlProjection.Project(bridge, "u3");
        Assert.That(activeAgent.UnitId, Is.EqualTo("u3"));
        Assert.That(activeAgent.AgentId, Is.EqualTo("a3"));
        Assert.That(activeAgent.StatusLabel, Is.Not.Empty);
        Assert.That(activeAgent.HasAgent, Is.True);
        Assert.That(activeAgent.IsAgentActive, Is.True);
        Assert.That(activeAgent.IsDetachedFromGroup, Is.False);
        Assert.That(activeAgent.ControllerKind, Is.EqualTo("Agent"));
        Assert.That(activeAgent.CanTakeControl, Is.True);
        Assert.That(activeAgent.CanPauseAgent, Is.True);
        Assert.That(activeAgent.CanResumeAgent, Is.False);
        Assert.That(activeAgent.CanReturnToAgent, Is.False);

        Assert.That(bridge.Orchestrator.TryPauseAgentActions(standalone.Target, 1.0), Is.True);
        var pausedAgent = RightUnitControlProjection.Project(bridge, "u3");
        Assert.That(pausedAgent.ControllerKind, Is.EqualTo("AgentPaused"));
        Assert.That(pausedAgent.IsAgentPaused, Is.True);
        Assert.That(pausedAgent.CanPauseAgent, Is.False);
        Assert.That(pausedAgent.CanResumeAgent, Is.True);
        Assert.That(pausedAgent.CanTakeControl, Is.True);

        Assert.That(bridge.Orchestrator.TryTakeDirectControl((UnitTarget)standalone.Target, 2.0), Is.True);
        var humanState = RightUnitControlProjection.Project(bridge, "u3");
        Assert.That(humanState.IsHumanControlled, Is.True);
        Assert.That(humanState.HasSuspendedAgent, Is.True);
        Assert.That(humanState.CanTakeControl, Is.False);
        Assert.That(humanState.CanReturnToAgent, Is.True);
        Assert.That(humanState.CanPauseAgent, Is.False);
        Assert.That(humanState.CanResumeAgent, Is.False);

        // Opposing AI unit
        var opposingState = RightUnitControlProjection.Project(bridge, "hostile-1");
        Assert.That(opposingState.IsPlayerControlled, Is.False);
        Assert.That(opposingState.IsDisabled, Is.True);
        Assert.That(opposingState.DisabledReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPlayerControlled));

        // AttachReplayViewer disables everything with explicit reason text
        bridge.AttachReplayViewer = true;
        var replayState = RightUnitControlProjection.Project(bridge, "u3");
        Assert.That(replayState.IsDisabled, Is.True);
        Assert.That(replayState.DisabledReasonCode, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(replayState.DisabledReason, Is.EqualTo(ObserverModeChromePresentation.ReplayAttachedReasonText));
        Assert.That(replayState.CanTakeControl, Is.False);
        Assert.That(replayState.CanReturnToAgent, Is.False);
        Assert.That(replayState.CanPauseAgent, Is.False);
        Assert.That(replayState.CanResumeAgent, Is.False);
    }
}
