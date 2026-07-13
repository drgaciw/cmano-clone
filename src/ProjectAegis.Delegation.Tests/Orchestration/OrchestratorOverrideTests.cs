namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class OrchestratorOverrideTests
{
    /// <summary>
    /// Adversarial: AttachReplayViewer must block take-control (observer / scrub only).
    /// Existing tests only pin TryEnqueueHumanOrder under the flag.
    /// </summary>
    [Test]
    public void TryTakeDirectControl_returns_false_when_AttachReplayViewer_enabled()
    {
        var orchestrator = new DelegationOrchestrator(1) { AttachReplayViewer = true };
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);

        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 1), Is.False);
        Assert.That(unit.Slot.Active, Is.SameAs(agent));
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void TryReleaseDirectControl_returns_false_when_AttachReplayViewer_enabled()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var unit = new UnitTarget(new TargetId("u1"));
        unit.Slot.SetActive(new HumanController());
        orchestrator.Register(unit);
        orchestrator.AttachReplayViewer = true;

        Assert.That(orchestrator.TryReleaseDirectControl(unit, simTime: 2), Is.False);
        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void TryTakeDirectControl_group_member_detaches_and_logs_events()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        group.AddMember(unit.Id);
        orchestrator.Register(group);
        orchestrator.Register(unit);

        group.Slot.SetActive(new AgentController(
            new AgentId("ga"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(1, 2),
            new StubPatrolPolicy(),
            attentionBudget: 20));

        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 5), Is.True);
        Assert.That(unit.IsDetachedFromGroup, Is.True);
        Assert.That(unit.DetachedFromGroupId, Is.EqualTo(group.Id));
        Assert.That(group.Members, Does.Not.Contain(unit.Id));
        Assert.That(group.PendingReplan, Is.True);
        Assert.That(unit.Slot.Active, Is.TypeOf<HumanController>());

        var entries = orchestrator.DecisionLog.ChronologicalEntries();
        Assert.That(entries.Any(e => e.Kind == OrderLogEntryKind.GroupMemberDetach), Is.True);
        Assert.That(entries.Any(e => e.Kind == OrderLogEntryKind.ControllerChange), Is.True);
    }

    [Test]
    public void TryTakeDirectControl_calledAgainOnAlreadyDetachedUnit_preservesQueuedPlayerOrders()
    {
        var orchestrator = new DelegationOrchestrator(4);
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        group.AddMember(unit.Id);
        orchestrator.Register(group);
        orchestrator.Register(unit);

        group.Slot.SetActive(new AgentController(
            new AgentId("ga"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(4, 5),
            new StubPatrolPolicy(),
            attentionBudget: 20));

        // Player takes direct control (e.g. detach button) once.
        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 1), Is.True);
        var human = unit.Slot.Active as HumanController;
        Assert.That(human, Is.Not.Null);

        // Player queues an order while comms delay holds it pending (req 19).
        human!.Enqueue(
            new Order(new OrderId(1), unit.Id, 1, OrderKind.Move, RiskLevel.Low),
            executeSimTick: 10);
        Assert.That(human.PendingOrderCount, Is.EqualTo(1));

        // Player hits "Take Direct Control" again (double-click / re-issued command) while
        // already detached. Because the unit is no longer a member of the group, the
        // FindParentGroup lookup misses and the orchestrator falls through to
        // unconditionally installing a brand-new HumanController, discarding the queue.
        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 2), Is.True);

        Assert.That(unit.Slot.Active, Is.SameAs(human),
            "Repeated take-control on an already-detached unit must not replace the existing HumanController.");
        Assert.That(human.PendingOrderCount, Is.EqualTo(1),
            "Queued player order must survive a redundant take-direct-control call.");
    }

    [Test]
    public void TryReleaseDirectControl_rejoins_detached_group_member()
    {
        var orchestrator = new DelegationOrchestrator(2);
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        group.AddMember(unit.Id);
        orchestrator.Register(group);
        orchestrator.Register(unit);

        group.Slot.SetActive(new AgentController(
            new AgentId("ga"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(2, 3),
            new StubPatrolPolicy(),
            attentionBudget: 20));

        orchestrator.TryTakeDirectControl(unit, simTime: 1);
        group.ClearReplanPending();

        Assert.That(orchestrator.TryReleaseDirectControl(unit, simTime: 2), Is.True);
        Assert.That(unit.IsDetachedFromGroup, Is.False);
        Assert.That(group.Members, Does.Contain(unit.Id));
        Assert.That(
            orchestrator.DecisionLog.ChronologicalEntries()
                .Any(e => e.Kind == OrderLogEntryKind.GroupMemberRejoin),
            Is.True);
    }
}
