namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Facade for Unity/DOTS: register entities, tick delegation, push orders to the sim.
/// </summary>
public sealed class DelegationBridge
{
    public DelegationBridge(int globalSeed, IPolicyEvaluator? policyEvaluator = null)
    {
        Orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        Registry = new TargetRegistry(Orchestrator);
    }

    public DelegationOrchestrator Orchestrator { get; }

    public TargetRegistry Registry { get; }

    public void ConfigureSimulationMode(
        SimulationModeProfile mode,
        IReadOnlyList<ICommandableTarget> friendly,
        IReadOnlyList<ICommandableTarget> opposing,
        TraitVector defaultTraits,
        AutonomyLevel agentAutonomy = AutonomyLevel.FullAutonomous)
    {
        SimulationModeConfigurator.Apply(
            Orchestrator,
            mode,
            friendly,
            opposing,
            defaultTraits,
            agentAutonomy);
    }

    public DelegationTickResult Tick(ISimWorldSnapshot snapshot, IOrderSink sink)
    {
        var observed = ObservedStateBuilder.Build(snapshot, Registry.CollectMemberIds());
        Orchestrator.Tick(observed);
        var dispatched = OrderDispatcher.Dispatch(Orchestrator.ExecutedOrders, Registry, sink);
        return new DelegationTickResult(Orchestrator.ExecutedOrders, dispatched);
    }

    public bool TryEnqueueHumanOrder(
        EntityKey entity,
        OrderKind kind,
        double simTime,
        RiskLevel? risk = null)
    {
        if (!Registry.TryGetBinding(entity, out var binding) ||
            binding.Target.Slot.Active is not HumanController human)
        {
            return false;
        }

        var resolvedRisk = risk ?? DefaultRiskClassifier.Classify(kind);
        human.Enqueue(new Order(
            new OrderId(0),
            binding.TargetId,
            simTime,
            kind,
            resolvedRisk));
        return true;
    }
}
