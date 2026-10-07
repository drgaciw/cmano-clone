namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>C3-01 / DRG-390: orchestrator enforces off-grid direct-order refusal (Sim authority).</summary>
[TestFixture]
public sealed class OrchestratorOffGridTests
{
    private static ObservedState State(double t) =>
        new(t, 0, 0, new Dictionary<TargetId, bool>());

    private static (DelegationOrchestrator Orchestrator, UnitTarget Unit, HumanController Human) Build()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            CommsGrid = new CommsGridRegistry(new[]
            {
                new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
            }),
        };
        var unit = new UnitTarget(new TargetId("u1"));
        var human = new HumanController();
        unit.Slot.SetActive(human);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();
        return (orchestrator, unit, human);
    }

    [Test]
    public void Order_issued_while_off_grid_is_dropped_with_OffGrid_denial()
    {
        var (orchestrator, _, human) = Build();
        orchestrator.Tick(State(10));
        human.Enqueue(new Order(new OrderId(1), new TargetId("u1"), 12, OrderKind.Hold, RiskLevel.Low), 12);

        orchestrator.Tick(State(12));

        Assert.That(orchestrator.ExecutedOrders, Is.Empty);
        var denial = orchestrator.DecisionLog.PolicyDenials.Single();
        Assert.That(denial.Reason, Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(denial.AgentId.Value, Is.EqualTo("comms-grid"));
        Assert.That(denial.AttemptedKind, Is.EqualTo(OrderKind.Hold));
        Assert.That(orchestrator.LastCommsGridChanges.Single().To, Is.EqualTo(CommsGridMembership.OffGrid));
    }

    [Test]
    public void Order_issued_before_leaving_grid_still_executes()
    {
        var (orchestrator, _, human) = Build();
        // Issued at T8 with a comms delay; executes at T11 after the unit went off grid at T10.
        human.Enqueue(new Order(new OrderId(1), new TargetId("u1"), 8, OrderKind.Hold, RiskLevel.Low), 11);

        orchestrator.Tick(State(11));

        Assert.That(orchestrator.ExecutedOrders, Has.Count.EqualTo(1));
        Assert.That(orchestrator.DecisionLog.PolicyDenials, Is.Empty);
    }

    [Test]
    public void Rejoined_unit_executes_new_orders()
    {
        var (orchestrator, _, human) = Build();
        orchestrator.Tick(State(20));
        human.Enqueue(new Order(new OrderId(1), new TargetId("u1"), 21, OrderKind.Hold, RiskLevel.Low), 21);

        orchestrator.Tick(State(21));

        Assert.That(orchestrator.ExecutedOrders, Has.Count.EqualTo(1));
    }

    [Test]
    public void Off_grid_unit_cannot_be_taken_under_direct_control()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            CommsGrid = new CommsGridRegistry(new[] { new ScenarioCommsGridTransition(5, "u2", "OffGrid") }),
        };
        var unit = new UnitTarget(new TargetId("u2"));
        orchestrator.Register(unit);

        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 6), Is.False);
        Assert.That(orchestrator.EvaluateOffGrid("u2", 6), Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void Comms_grid_resolves_lazily_from_scenario_policy()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree)
            {
                CommsGridTransitions = new[] { new ScenarioCommsGridTransition(1, "u1", "OffGrid") },
            },
        };
        Assert.That(orchestrator.CommsGrid, Is.Not.Null);
        Assert.That(new DelegationOrchestrator(1).CommsGrid, Is.Null);
    }
}
