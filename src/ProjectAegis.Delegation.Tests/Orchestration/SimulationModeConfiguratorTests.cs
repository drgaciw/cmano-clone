namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class SimulationModeConfiguratorTests
{
    [Test]
    public void Human_mode_assigns_human_to_friendly_and_agent_to_opposing()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Human, PlayerControlsFriendlySide: true),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void AgentVsAgent_mode_assigns_agents_to_both_sides()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.AgentVsAgent, PlayerControlsFriendlySide: false),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<AgentController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }
}
