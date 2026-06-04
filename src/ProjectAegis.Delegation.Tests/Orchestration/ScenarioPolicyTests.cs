using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class ScenarioPolicyTests
{
    [Test]
    public void Scenario_policy_id_assigns_opposing_hold_fire()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            ScenarioPolicy = ScenarioPolicyCatalog.BalticPatrolOpposingHoldFire,
        };

        var opp = new UnitTarget(new TargetId("opp-1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("opp-0"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, opp, effectivePolicy: null, isFriendly: false);

        Assert.That(agent.EffectivePolicy.Roe, Is.EqualTo(RoeLevel.HoldFire));
    }

    [Test]
    public void Mission_roe_inherits_over_side_default_on_assign()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["f1"]);
        var orchestrator = new DelegationOrchestrator(1) { ScenarioPolicy = profile };
        var unit = new UnitTarget(new TargetId("f1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, effectivePolicy: null, isFriendly: true);

        Assert.That(agent.EffectivePolicy.Roe, Is.EqualTo(RoeLevel.WeaponsTight));
    }

    [Test]
    public void Configurator_applies_scenario_policy_from_catalog()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var friendly = new[] { new UnitTarget(new TargetId("f1")) };
        var opposing = new[] { new UnitTarget(new TargetId("o1")) };

        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Human, PlayerControlsFriendlySide: true),
            friendly,
            opposing,
            PersonalityCatalog.All[0].Traits,
            scenarioPolicyId: "baltic-patrol-opp-hold-fire");

        var oppAgent = (AgentController)opposing[0].Slot.Active!;
        Assert.That(oppAgent.EffectivePolicy.Roe, Is.EqualTo(RoeLevel.HoldFire));
    }
}
