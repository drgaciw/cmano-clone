namespace ProjectAegis.Delegation.Tests.Controllers;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class OverrideTests
{
    [Test]
    public void Override_swaps_agent_for_human_and_preserves_suspended_agent()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = new AgentController(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(1, 1),
            new StubPatrolPolicy(),
            attentionBudget: 20);
        unit.Slot.SetActive(agent);

        var service = new OverrideService();
        service.TakeDirectControl(unit, new HumanController());

        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(agent));
    }

    [Test]
    public void Release_restores_suspended_agent()
    {
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = new AgentController(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(1, 1),
            new StubPatrolPolicy(),
            attentionBudget: 20);
        unit.Slot.SetActive(agent);
        var service = new OverrideService();
        service.TakeDirectControl(unit, new HumanController());

        service.ReleaseDirectControl(unit);

        Assert.That(unit.Slot.Active, Is.SameAs(agent));
        Assert.That(unit.Slot.SuspendedAgent, Is.Null);
    }
}
