using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;

const int seed = 42;
var orchestrator = new DelegationOrchestrator(seed);

var friendlyUnit = new UnitTarget(new TargetId("friendly-1"));
var opposingUnit = new UnitTarget(new TargetId("opposing-1"));
        orchestrator.Register(friendlyUnit);
        orchestrator.Register(opposingUnit);

        var traits = PersonalityCatalog.All.First(p => p.Name == "Cautious").Traits;
        SimulationModeConfigurator.Apply(
            orchestrator,
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendlyUnit],
            opposing: [opposingUnit],
            traits);
        orchestrator.BeginExecution();

if (friendlyUnit.Slot.Active is HumanController human)
{
    human.Enqueue(new Order(
        new OrderId(0),
        friendlyUnit.Id,
        SimTime: 0,
        OrderKind.Hold,
        RiskLevel.Low));
}

var state0 = new ObservedState(0, ContactCount: 1, ActiveEngagementCount: 0, new Dictionary<TargetId, bool>());
var state1 = new ObservedState(1, ContactCount: 3, ActiveEngagementCount: 1, new Dictionary<TargetId, bool>());

orchestrator.Tick(state0);
orchestrator.Tick(state1);

Console.WriteLine($"Project Aegis Delegation Demo (seed={seed})");
Console.WriteLine($"Executed orders: {orchestrator.ExecutedOrders.Count}");
foreach (var order in orchestrator.ExecutedOrders)
{
    Console.WriteLine($"  t={order.SimTime:F0} {order.Target.Value} -> {order.Kind}");
}

Console.WriteLine($"Decision log entries: {orchestrator.DecisionLog.Records.Count}");
foreach (var record in orchestrator.DecisionLog.Records)
{
    Console.WriteLine($"  agent={record.AgentId.Value} chose {record.ChosenKind} (load {record.AttentionLoad:F1}/{record.AttentionBudget:F1})");
}
