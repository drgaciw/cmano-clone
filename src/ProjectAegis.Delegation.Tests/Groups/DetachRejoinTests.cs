namespace ProjectAegis.Delegation.Tests.Groups;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Groups;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class DetachRejoinTests
{
    [Test]
    public void Detach_marks_unit_and_schedules_group_replan_next_cycle()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        group.AddMember(unit.Id);
        group.Slot.SetActive(new AgentController(
            new AgentId("ga"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(1, 2),
            new StubPatrolPolicy(),
            attentionBudget: 20));

        var service = new DetachRejoinService(new OverrideService());
        service.Detach(group, unit);

        Assert.That(unit.IsDetachedFromGroup, Is.True);
        Assert.That(group.PendingReplan, Is.True);
        Assert.That(group.Members, Does.Not.Contain(unit.Id));
    }

    [Test]
    public void Rejoin_adds_member_and_clears_detach_flag()
    {
        var group = new GroupTarget(new TargetId("g1"));
        var unit = new UnitTarget(new TargetId("u1"));
        var service = new DetachRejoinService(new OverrideService());
        service.Detach(group, unit);

        service.Rejoin(group, unit);

        Assert.That(unit.IsDetachedFromGroup, Is.False);
        Assert.That(group.Members, Does.Contain(unit.Id));
    }
}
