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
