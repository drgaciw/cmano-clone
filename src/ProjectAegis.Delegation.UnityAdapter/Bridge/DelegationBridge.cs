namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Facade for Unity/DOTS: register entities, tick delegation, push orders to the sim.
/// </summary>
public sealed class DelegationBridge
{
    private SimulationSession? _simSession;
    private readonly List<Order> _nonEngageOrdersCache = new();

    public DelegationBridge(int globalSeed, IPolicyEvaluator? policyEvaluator = null)
    {
        Orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        Registry = new TargetRegistry(Orchestrator);
    }

    public DelegationOrchestrator Orchestrator { get; }

    public TargetRegistry Registry { get; }

    public SimulationSession? SimSession => _simSession;

    /// <summary>Route <see cref="OrderKind.Engage"/> through <see cref="SimulationSession"/> (DLZ/magazines MVP).</summary>
    public DelegationBridge EnableMvpEngagement(
        EngageContext? defaultEngageContext = null,
        int defaultMagazineRounds = 2)
    {
        _simSession = SimulationSession.BindMvpEngagement(
            Orchestrator,
            defaultEngageContext ?? new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 2,
                HasFireControlTrack: true),
            defaultMagazineRounds);
        return this;
    }

    public SimulationPhase Phase => Orchestrator.Phase;

    public void BeginExecution() => Orchestrator.BeginExecution();

    public IReadOnlyList<OrderLogEntry> GetLiveOrderLogView() =>
        Orchestrator.GetLiveOrderLogView();

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

        if (_simSession != null)
        {
            if (!_simSession.Tick(observed))
            {
                Orchestrator.Tick(observed);
                return new DelegationTickResult(Array.Empty<Order>(), 0);
            }

            var orders = Orchestrator.ExecutedOrders;
            _nonEngageOrdersCache.Clear();
            foreach (var order in orders)
            {
                if (order.Kind != OrderKind.Engage)
                {
                    _nonEngageOrdersCache.Add(order);
                }
            }

            var dispatched = OrderDispatcher.Dispatch(_nonEngageOrdersCache, Registry, sink);
            return new DelegationTickResult(
                orders,
                dispatched,
                _simSession.Sim.LastEngagementResults.Count);
        }

        Orchestrator.Tick(observed);
        var allDispatched = OrderDispatcher.Dispatch(Orchestrator.ExecutedOrders, Registry, sink);
        return new DelegationTickResult(Orchestrator.ExecutedOrders, allDispatched);
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
