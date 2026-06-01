namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
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
        Assert.That(orchestrator.Phase, Is.EqualTo(SimulationPhase.Executing));
    }

    [Test]
    public void Human_mode_stays_in_planning_until_begin_execution()
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

        Assert.That(orchestrator.Phase, Is.EqualTo(SimulationPhase.Planning));
    }

    [Test]
    public void Mixed_mode_with_dual_side_policy_assigns_human_to_both_sides()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                allowDualSideControl: true),
        };
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Mixed_mode_without_dual_side_policy_keeps_one_side_human()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<AgentController>());
    }

    [Test]
    public void Mixed_mode_PlayerControlsFriendlySide_false_assigns_agent_to_friendly_and_human_to_opposing()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: false),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<AgentController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Mixed_mode_dual_side_policy_ignores_PlayerControlsFriendlySide_false()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                allowDualSideControl: true),
        };
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: false),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<HumanController>());
    }

    [Test]
    public void Mixed_mode_stays_in_planning_phase()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits);

        Assert.That(orchestrator.Phase, Is.EqualTo(SimulationPhase.Planning));
    }

    [Test]
    public void Mixed_mode_with_scenarioPolicyId_resolving_to_dual_side_assigns_human_to_both_sides()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new UnitTarget(new TargetId("f1"));
        var opposing = new UnitTarget(new TargetId("o1"));

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly],
            [opposing],
            PersonalityCatalog.All[0].Traits,
            scenarioPolicyId: "test-sandbox-dual-side");

        Assert.That(orchestrator.ScenarioPolicy?.AllowDualSideControl, Is.True);
        Assert.That(friendly.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(opposing.Slot.Active, Is.InstanceOf<HumanController>());
    }
}
