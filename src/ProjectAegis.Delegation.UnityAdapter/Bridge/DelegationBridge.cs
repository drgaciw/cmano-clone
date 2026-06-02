namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Trust;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Facade for Unity/DOTS: register entities, tick delegation, push orders to the sim.
/// When <see cref="Session"/> is set, ticks run the MVP engage pipeline after delegation.
/// </summary>
public sealed class DelegationBridge
{
    private readonly List<Order> _nonEngageOrdersCache = new();

    public DelegationBridge(
        int globalSeed,
        IPolicyEvaluator? policyEvaluator = null,
        bool mvpEngagement = false,
        string? scenarioPolicyId = null)
    {
        Orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        Registry = new TargetRegistry(Orchestrator);
        if (!string.IsNullOrWhiteSpace(scenarioPolicyId))
        {
            ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
            Orchestrator.ScenarioPolicy = ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        }

        Session = mvpEngagement
            ? SimulationSession.BindMvpEngagementForScenario(Orchestrator, scenarioPolicyId)
            : null;
    }

    /// <summary>Headless engage session sharing <see cref="Orchestrator"/> (null when MVP engage disabled).</summary>
    public SimulationSession? Session { get; private set; }

    public DelegationOrchestrator Orchestrator { get; }

    public TargetRegistry Registry { get; }

    /// <summary>Alias for <see cref="Session"/> (main-line compat).</summary>
    public SimulationSession? SimSession => Session;

    /// <summary>Enable MVP engage after construction (Unity host opt-in).</summary>
    public DelegationBridge EnableMvpEngagement(
        EngageContext? defaultEngageContext = null,
        int defaultMagazineRounds = 2)
    {
        Session = SimulationSession.BindMvpEngagement(
            Orchestrator,
            defaultEngageContext ?? DefaultEngageContext,
            defaultMagazineRounds);
        return this;
    }

    public SimulationPhase Phase => Orchestrator.Phase;

    public void BeginExecution() => Orchestrator.BeginExecution();

    public bool AttachReplayViewer
    {
        get => Orchestrator.AttachReplayViewer;
        set => Orchestrator.AttachReplayViewer = value;
    }

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

        if (Session != null)
        {
            if (!Session.Tick(observed))
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
                Session.Sim.LastEngagementResults.Count);
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
        if (Orchestrator.AttachReplayViewer)
        {
            return false;
        }

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

    public bool TryTakeDirectControl(EntityKey entity, double simTime)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding) ||
            binding.Target is not UnitTarget unit)
        {
            return false;
        }

        return Orchestrator.TryTakeDirectControl(unit, simTime);
    }

    public bool TryReleaseDirectControl(EntityKey entity, double simTime)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding) ||
            binding.Target is not UnitTarget unit)
        {
            return false;
        }

        return Orchestrator.TryReleaseDirectControl(unit, simTime);
    }

    public IReadOnlyList<TrustSignal> FinalizeScenario(
        bool missionSucceeded = false,
        double objectivesMetRatio = 1.0) =>
        Orchestrator.FinalizeScenario(missionSucceeded, objectivesMetRatio);

    private static EngageContext DefaultEngageContext { get; } = new(
        50_000,
        new WeaponEnvelope(1_000, 100_000),
        RoundsRemaining: 2,
        HasFireControlTrack: true);
}