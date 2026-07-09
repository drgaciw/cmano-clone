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
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Data.Catalog;
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

    // ADR-019 session state (NOT order-log / fingerprint): agent pause + logged C2 intents.
    // Golden Baltic path never pauses/resumes/changes autonomy → hash unchanged.
    private readonly HashSet<string> _pausedAgentUnitIds = new(StringComparer.Ordinal);
    private readonly List<AgentPauseRequested> _agentPauseIntents = new();
    private readonly List<AgentResumeRequested> _agentResumeIntents = new();
    private readonly List<AutonomyLevelChangeRequested> _autonomyChangeIntents = new();

    public DelegationBridge(
        int globalSeed,
        IPolicyEvaluator? policyEvaluator = null,
        bool mvpEngagement = false,
        string? scenarioPolicyId = null,
        ICatalogReader? catalog = null)
    {
        Orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        Registry = new TargetRegistry(Orchestrator);
        if (!string.IsNullOrWhiteSpace(scenarioPolicyId))
        {
            ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
            Orchestrator.ScenarioPolicy = ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        }

        var catalogReader = catalog
            ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
            ?? InMemoryCatalogReader.BalticPatrolFixture();
        Session = mvpEngagement
            ? SimulationSession.BindMvpEngagementForScenario(Orchestrator, scenarioPolicyId, catalogReader)
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
        int defaultMagazineRounds = 2,
        ICatalogReader? catalog = null)
    {
        var engage = CatalogEngageEnvelope.Apply(
            defaultEngageContext ?? DefaultEngageContext,
            catalog ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
                ?? InMemoryCatalogReader.BalticPatrolFixture());
        Session = SimulationSession.BindMvpEngagement(
            Orchestrator,
            engage,
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

    /// <summary>
    /// Cancel the queued/plotted human order for <paramref name="entity"/> before it executes, emitting
    /// a logged <c>PlayerOrderCancelled</c> intent (req 20 rev 2 §Order lifecycle, TR-c2-006). ADDITIVE
    /// to the bridge command API — introduces no change to any existing method, so the CRITICAL upstream
    /// surface is unaffected; and it only appends a log entry when a player actually cancels, so replays
    /// that never cancel keep an identical order-log fingerprint (Baltic hash unchanged). No-op in replay.
    /// </summary>
    /// <returns>True if a pending order was cancelled; otherwise false with <paramref name="failureReason"/> set.</returns>
    public bool TryCancelHumanOrder(EntityKey entity, double simTime, out string? failureReason)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            failureReason = "replay";
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding) ||
            binding.Target.Slot.Active is not HumanController human)
        {
            failureReason = "no-human-controller";
            return false;
        }

        if (!human.TryCancel(binding.TargetId, out var cancelled, out var executeTick))
        {
            failureReason = "no-pending-order";
            return false;
        }

        var simTick = (ulong)Math.Max(0, (long)simTime);
        Orchestrator.DecisionLog.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0,
            simTime,
            simTick,
            binding.TargetId,
            cancelled.Kind,
            CancelledExecuteSimTick: executeTick));
        failureReason = null;
        return true;
    }

    /// <summary>
    /// ADR-019 / Req 20 P0: pause agent decisions for <paramref name="entity"/> and push
    /// <see cref="PauseReasonIds.AgentGate"/> onto the optional T5 pause stack. ADDITIVE —
    /// no Tick/hotpath change. Session-only state (not order-log), so Baltic goldens that never
    /// pause keep an identical fingerprint. No-op in replay.
    /// </summary>
    /// <returns>True if the unit was newly paused; otherwise false with <paramref name="failureReason"/> set.</returns>
    public bool TryPauseAgent(
        EntityKey entity,
        double simTime,
        PauseReasonStack? pauseStack,
        out string? failureReason)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            failureReason = "replay";
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = "unknown-entity";
            return false;
        }

        if (binding.Target.Slot.Active is not AgentController)
        {
            failureReason = "no-active-agent";
            return false;
        }

        var unitId = binding.TargetId.Value;
        if (!_pausedAgentUnitIds.Add(unitId))
        {
            failureReason = "already-paused";
            return false;
        }

        if (_pausedAgentUnitIds.Count == 1)
        {
            pauseStack?.Push(PauseReasonIds.AgentGate);
        }

        _agentPauseIntents.Add(new AgentPauseRequested(unitId, simTime));
        failureReason = null;
        return true;
    }

    /// <summary>
    /// ADR-019 / Req 20 P0: resume a previously paused agent and pop
    /// <see cref="PauseReasonIds.AgentGate"/> when no units remain paused. ADDITIVE — no Tick change.
    /// No-op in replay.
    /// </summary>
    public bool TryResumeAgent(
        EntityKey entity,
        double simTime,
        PauseReasonStack? pauseStack,
        out string? failureReason)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            failureReason = "replay";
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = "unknown-entity";
            return false;
        }

        var unitId = binding.TargetId.Value;
        if (!_pausedAgentUnitIds.Remove(unitId))
        {
            failureReason = "not-paused";
            return false;
        }

        if (_pausedAgentUnitIds.Count == 0)
        {
            pauseStack?.Remove(PauseReasonIds.AgentGate);
        }

        _agentResumeIntents.Add(new AgentResumeRequested(unitId, simTime));
        failureReason = null;
        return true;
    }

    /// <summary>
    /// ADR-019 / Req 20 P0: set autonomy for the agent on <paramref name="entity"/> (active or
    /// suspended under C5 override). ADDITIVE — no Tick change. Logs
    /// <see cref="AutonomyLevelChangeRequested"/> session intent only (not order-log). No-op in replay.
    /// </summary>
    public bool TrySetAutonomyLevel(
        EntityKey entity,
        AutonomyLevel autonomyLevel,
        double simTime,
        out string? failureReason)
    {
        if (Orchestrator.AttachReplayViewer)
        {
            failureReason = "replay";
            return false;
        }

        if (!Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = "unknown-entity";
            return false;
        }

        var agent = binding.Target.Slot.Active as AgentController
            ?? binding.Target.Slot.SuspendedAgent;
        if (agent is null)
        {
            failureReason = "no-agent";
            return false;
        }

        if (agent.Autonomy == autonomyLevel)
        {
            failureReason = "unchanged";
            return false;
        }

        agent.Autonomy = autonomyLevel;
        _autonomyChangeIntents.Add(new AutonomyLevelChangeRequested(
            binding.TargetId.Value,
            autonomyLevel,
            simTime));
        failureReason = null;
        return true;
    }

    /// <summary>True when the unit is in the ADR-019 agent-pause set (session/UI state).</summary>
    public bool IsAgentPaused(string unitId) =>
        !string.IsNullOrEmpty(unitId) && _pausedAgentUnitIds.Contains(unitId);

    /// <summary>Logged <see cref="AgentPauseRequested"/> intents (session; not order-log fingerprint).</summary>
    public IReadOnlyList<AgentPauseRequested> AgentPauseIntents => _agentPauseIntents;

    /// <summary>Logged <see cref="AgentResumeRequested"/> intents (session; not order-log fingerprint).</summary>
    public IReadOnlyList<AgentResumeRequested> AgentResumeIntents => _agentResumeIntents;

    /// <summary>Logged <see cref="AutonomyLevelChangeRequested"/> intents (session; not order-log fingerprint).</summary>
    public IReadOnlyList<AutonomyLevelChangeRequested> AutonomyChangeIntents => _autonomyChangeIntents;

    /// <summary>
    /// ADR-019 read projection for one entity from controller ownership + session pause set.
    /// </summary>
    public bool TryProjectDelegationState(EntityKey entity, out DelegationStateProjection? projection)
    {
        if (!Registry.TryGetBinding(entity, out var binding))
        {
            projection = null;
            return false;
        }

        var unitId = binding.TargetId.Value;
        projection = DelegationStateProjectionBuilder.FromSlot(
            unitId,
            binding.Target.Slot,
            paused: _pausedAgentUnitIds.Contains(unitId));
        return true;
    }

    /// <summary>Project all registered bindings for badge / OOB ownership filters.</summary>
    public IReadOnlyList<DelegationStateProjection> ProjectAllDelegationStates()
    {
        var result = new List<DelegationStateProjection>(Registry.Bindings.Count);
        foreach (var binding in Registry.Bindings)
        {
            var unitId = binding.TargetId.Value;
            result.Add(DelegationStateProjectionBuilder.FromSlot(
                unitId,
                binding.Target.Slot,
                paused: _pausedAgentUnitIds.Contains(unitId)));
        }

        return result;
    }

    /// <summary>Attack menu entries for UI binding (live engage context).</summary>
    public IReadOnlyList<EngageAttackOptions.AttackOption> GetAttackMenuOptions(
        string unitId,
        ISimWorldSnapshot snapshot)
    {
        if (!TryResolveEntityKey(unitId, out var entity))
        {
            return Array.Empty<EngageAttackOptions.AttackOption>();
        }

        var engageDefaults = Orchestrator.ScenarioPolicy?.EngageDefaults
            ?? ScenarioEngageDefaults.MvpFallback;
        var ctx = BuildLiveEngageContext(snapshot, unitId, engageDefaults);
        var preview = EngagePreviewProjection.Project(in ctx, engageDefaults.DlzPersonality);
        return EngageAttackOptions.Build(in ctx, preview);
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

    private static bool TryResolveEntityKey(
        TargetRegistry registry,
        string unitId,
        out EntityKey entityKey)
    {
        entityKey = default;
        foreach (var binding in registry.Bindings)
        {
            if (string.Equals(binding.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                entityKey = binding.Entity;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveEntityKey(string unitId, out EntityKey entityKey) =>
        TryResolveEntityKey(Registry, unitId, out entityKey);

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