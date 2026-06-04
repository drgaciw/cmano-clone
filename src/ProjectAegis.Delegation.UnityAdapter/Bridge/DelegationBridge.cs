namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Logistics;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
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
    private readonly CommsTimelineSimulator? _commsTimeline;
    private readonly SpoofTrackTimelineSimulator? _spoofTimeline;
    private readonly FuelTimelineTracker? _fuelTimeline;

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
        _commsTimeline = CommsTimelineSimulator.TryCreate(Orchestrator.ScenarioPolicy);
        _spoofTimeline = SpoofTrackTimelineSimulator.TryCreate(Orchestrator.ScenarioPolicy);
        _fuelTimeline = FuelTimelineTracker.TryCreate(Orchestrator.ScenarioPolicy);
        ApplyScenarioRuntimeBindings();
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
        ApplyScenarioRuntimeBindings();
        return this;
    }

    public SimulationPhase Phase => Orchestrator.Phase;

    public CommsState CurrentCommsState => _commsTimeline?.CurrentState ?? CommsState.Nominal;

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
        EmitCommsTransitions(snapshot);
        AdvanceSpoofTimeline(snapshot);
        EmitFuelTransitions(snapshot);
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
        var simTick = (ulong)Math.Max(0, (long)simTime);
        var commsDisplay = Orchestrator.ScenarioPolicy?.CommsDisplay ?? ScenarioCommsDisplaySettings.Default;
        var executeTick = CommsOrderDelay.ComputeExecuteSimTick(simTick, CurrentCommsState, commsDisplay);
        human.Enqueue(
            new Order(
                new OrderId(0),
                binding.TargetId,
                simTime,
                kind,
                resolvedRisk),
            executeTick);
        Orchestrator.DecisionLog.AppendPlayerOrder(new PlayerOrderRecord(
            0,
            simTime,
            simTick,
            binding.TargetId,
            kind,
            ExecuteSimTick: executeTick));
        return true;
    }

    /// <summary>Interactive attack menu → player order (req 14 / doc 20).</summary>
    public bool TryEnqueueAttackOption(
        EntityKey entity,
        string optionId,
        ISimWorldSnapshot snapshot,
        out string? failureReason)
    {
        failureReason = null;
        var shooterId = Registry.TryGetBinding(entity, out var binding)
            ? binding.TargetId.Value
            : null;
        if (shooterId == null)
        {
            failureReason = "UNKNOWN_UNIT";
            return false;
        }

        var engageDefaults = Orchestrator.ScenarioPolicy?.EngageDefaults
            ?? ScenarioEngageDefaults.MvpFallback;
        var ctx = BuildLiveEngageContext(snapshot, shooterId, engageDefaults);
        var preview = EngagePreviewProjection.Project(in ctx, engageDefaults.DlzPersonality);
        if (!EngageAttackOrderResolver.TryResolve(optionId, in ctx, preview, out var resolved, out failureReason))
        {
            return false;
        }

        if (Session != null && resolved.SalvoSize is int salvo)
        {
            Session.NextEngageSalvoOverride = salvo;
        }

        return TryEnqueueHumanOrder(entity, resolved.Kind, snapshot.SimTime);
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

    private void ApplyScenarioRuntimeBindings()
    {
        if (Session == null)
        {
            return;
        }

        var profile = Orchestrator.ScenarioPolicy;
        if (profile is { UnitReadiness.Count: > 0 })
        {
            Session.UnitReadiness = new UnitReadinessMap(profile.UnitReadiness);
        }

        if (_spoofTimeline != null)
        {
            Session.IsContactSpoofed = (contactId, _) => _spoofTimeline.IsSpoofed(contactId);
        }
    }

    private EngageContext BuildLiveEngageContext(
        ISimWorldSnapshot snapshot,
        string shooterUnitId,
        ScenarioEngageDefaults engageDefaults)
    {
        var ctx = engageDefaults.ToEngageContext(engageDefaults.DefaultMagazineRounds);
        var airReady = Session?.UnitReadiness?.IsReadyForLaunch(shooterUnitId) ?? true;
        var victim = snapshot.PrimaryHostileContactId?.Value;
        var spoofed = _spoofTimeline?.IsSpoofed(victim) ?? false;
        return ctx with
        {
            HasFireControlTrack = snapshot.HasFireControlTrackOnPrimaryContact,
            RadarEmconActive = snapshot.ObserverRadarEmconActive,
            AirOperationsReady = airReady,
            TrackSpoofed = spoofed,
        };
    }

    private void AdvanceSpoofTimeline(ISimWorldSnapshot snapshot)
    {
        if (_spoofTimeline == null)
        {
            return;
        }

        var simTick = (ulong)Math.Max(0, (long)snapshot.SimTime);
        _spoofTimeline.Advance(simTick);
    }

    private void EmitCommsTransitions(ISimWorldSnapshot snapshot)
    {
        if (_commsTimeline == null)
        {
            return;
        }

        var simTick = (ulong)Math.Max(0, (long)snapshot.SimTime);
        foreach (var change in _commsTimeline.Drain(simTick, snapshot.SimTime))
        {
            Orchestrator.DecisionLog.AppendCommsStateChange(change);
        }
    }

    private void EmitFuelTransitions(ISimWorldSnapshot snapshot)
    {
        if (_fuelTimeline == null)
        {
            return;
        }

        var simTick = (ulong)Math.Max(0, (long)snapshot.SimTime);
        var unitIds = Registry.CollectMemberIds();
        if (unitIds.Count == 0)
        {
            return;
        }

        var drain = _fuelTimeline.Drain(simTick, snapshot.SimTime, 1.0, unitIds);
        foreach (var burn in drain.Burns)
        {
            Orchestrator.DecisionLog.AppendFuelBurn(burn);
        }

        foreach (var change in drain.BandChanges)
        {
            Orchestrator.DecisionLog.AppendFuelStateChange(change);
        }
    }

    private static EngageContext DefaultEngageContext { get; } = new(
        50_000,
        new WeaponEnvelope(1_000, 100_000),
        RoundsRemaining: 2,
        HasFireControlTrack: true);
}