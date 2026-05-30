namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

[TestFixture]
public sealed class OrchestratorTests
{
    [Test]
    public void Two_ticks_same_seed_produce_identical_executed_orders()
    {
        var run1 = RunScenario(globalSeed: 1234);
        var run2 = RunScenario(globalSeed: 1234);
        Assert.That(run1, Is.EqualTo(run2));
    }

    private static IReadOnlyList<OrderKind> RunScenario(int globalSeed)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0, new Dictionary<TargetId, bool>());
        orchestrator.Tick(state);
        orchestrator.Tick(state with { SimTime = 1 });
        return orchestrator.ExecutedOrders.Select(o => o.Kind).ToArray();
    }
}
