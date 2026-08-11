namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Logistics;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using ProjectAegis.Sim.Telemetry;
using ProjectAegis.Sim.Time;
using ProjectAegis.Delegation.Logistics;

/// <summary>Headless/interactive session: delegation tick then sim engagement phase.</summary>
public sealed class SimulationSession
{
    public SimulationSession(
        int globalSeed,
        IEngagementResolver? engagement = null,
        IPolicyEvaluator? policyEvaluator = null)
    {
        var seed = SimSeed.FromScenario((ulong)globalSeed);
        Orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        Sim = new SimTickPipeline(seed, engagement ?? new StubEngagementResolver());
    }

    private SimulationSession(DelegationOrchestrator orchestrator, SimTickPipeline sim)
    {
        Orchestrator = orchestrator;
        Sim = sim;
    }

    public static SimulationSession BindMvpEngagement(
        DelegationOrchestrator orchestrator,
        EngageContext defaultEngageContext,
        int defaultMagazineRounds = 2,
        ICatalogReader? catalogReader = null)
    {
        var seed = SimSeed.FromScenario((ulong)orchestrator.GlobalSeed);
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        var killedTargets = new KilledTargetRegistry();
        var speculative = orchestrator.ScenarioPolicy?.Speculative
            ?? ScenarioSpeculativeSettings.CampaignDefault;
        var engageDefaults = orchestrator.ScenarioPolicy?.EngageDefaults
            ?? ScenarioEngageDefaults.MvpFallback;
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            orchestrator.PolicyEvaluator,
            orchestrator.ResolveEffectivePolicyForUnit,
            seed,
            killedTargets,
            speculative,
            engageDefaults.CombatDomainsEnabled);
        var sim = new SimTickPipeline(seed, resolver);
        return new SimulationSession(orchestrator, sim)
        {
            EngageWorld = world,
            Magazines = magazines,
            KilledTargets = killedTargets,
            MvpResolver = resolver,
            DefaultEngageContext = defaultEngageContext,
            DefaultMagazineRounds = defaultMagazineRounds,
            CatalogReader = catalogReader,
            BalanceDriftConsumer = new BalanceDriftAdvisoryConsumer(orchestrator.ScenarioPolicy?.BalanceTelemetry),
            CatalogDamageHotTickTracker = CatalogDamageHotTickTracker.TryCreate(
                orchestrator.ScenarioPolicy,
                engageDefaults.CombatDomainsEnabled,
                catalogReader,
                orchestrator.GlobalSeed),
            BdaContactLifecycleRegistry = BdaContactLifecycleHotTickApplier.IsEnabled(engageDefaults.CombatDomainsEnabled)
                ? new BdaContactLifecycleRegistry()
                : null,
        };
    }

    public static SimulationSession BindMvpEngagementForScenario(
        DelegationOrchestrator orchestrator,
        string? scenarioPolicyId,
        ICatalogReader? catalog = null,
        string weaponId = CatalogWeaponIds.MvpDefault)
    {
        var profile = string.IsNullOrWhiteSpace(scenarioPolicyId)
            ? null
            : ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        var engage = profile?.ResolveEngageContext()
            ?? ScenarioEngageDefaults.MvpFallback.ToEngageContext(
                ScenarioEngageDefaults.MvpFallback.DefaultMagazineRounds);
        engage = CatalogEngageEnvelope.Apply(engage, catalog, weaponId);
        var rounds = profile?.EngageDefaults?.DefaultMagazineRounds
            ?? ScenarioEngageDefaults.MvpFallback.DefaultMagazineRounds;
        return BindMvpEngagement(orchestrator, engage, rounds, catalog);
    }

    public SimulationPhase Phase => Orchestrator.Phase;

    public bool AttachReplayViewer
    {
        get => Orchestrator.AttachReplayViewer;
        set => Orchestrator.AttachReplayViewer = value;
    }

    public DelegationOrchestrator Orchestrator { get; }

    public SimTickPipeline Sim { get; }

    public WatchAttentionQueue WatchQueue { get; } = new();

    public WatchAutoPauseGate WatchPauseGate { get; } = new();

    public WatchPauseReason LastWatchPauseReason => WatchPauseGate.LastPauseReason;

    public bool IsSimPaused => Sim.Clock.IsPaused;

    public void PauseSim() => Sim.Clock.Pause();

    public void ResumeSim() => Sim.Clock.Resume();

    public bool TryResumeSim(bool explicitOverride = false)
    {
        if (!WatchPauseGate.CanResume(WatchQueue, explicitOverride))
        {
            return false;
        }

        ResumeSim();
        WatchPauseGate.ClearReason();
        return true;
    }

    public void ReportWatchAttention(WatchAttentionEvent evt)
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        WatchQueue.Enqueue(evt);
        if (WatchPauseGate.ShouldAutoPause(evt))
        {
            PauseSim();
        }
    }

    /// <summary>
    /// S116: map contact transitions to watch events (first hostile/unknown + own-side Lost).
    /// Pure factory; idempotent EventIds. Call from harness/sensor path without Bridge edits.
    /// </summary>
    public void ReportContactTransitions(IReadOnlyList<ContactTransition> transitions)
    {
        if (transitions is null || transitions.Count == 0)
        {
            return;
        }

        for (var i = 0; i < transitions.Count; i++)
        {
            var t = transitions[i];
            if (WatchAttentionEmitFactory.TryFromFirstHostileOrUnknownContact(in t, out var contactEvt)
                && contactEvt is not null)
            {
                ReportWatchAttention(contactEvt);
            }

            if (WatchAttentionEmitFactory.TryFromOwnSideLostTransition(in t, out var lossEvt)
                && lossEvt is not null)
            {
                ReportWatchAttention(lossEvt);
            }
        }
    }

    /// <summary>
    /// S116: report own-side unit loss (BDA / battle-damage). No-op for non-own-side ids.
    /// </summary>
    public void ReportOwnSideLoss(string unitId, ulong triggerTick, string? reasonDetail = null)
    {
        if (WatchAttentionEmitFactory.TryFromOwnSideLoss(unitId, triggerTick, reasonDetail, out var evt)
            && evt is not null)
        {
            ReportWatchAttention(evt);
        }
    }

    public int TimeAccelerationFactor => Sim.Clock.AccelerationFactor;

    public void SetTimeAccelerationFactor(int factor) => Sim.Clock.SetAccelerationFactor(factor);

    public void BeginExecution() => Orchestrator.BeginExecution();

    public bool Tick(ObservedState state)
    {
        if (Orchestrator.Phase == SimulationPhase.Planning)
        {
            return false;
        }

        RunExecutingTick(state, headlessOverride: false);
        return true;
    }

    /// <summary>
    /// S115: headless/CI path. Advances the engagement pipeline even when the clock is paused.
    /// Pause flag and watch reason are preserved so interactive resume still works after batch.
    /// Mirrors <see cref="TimeCompressionMode.HeadlessBatch"/> semantics at session level.
    /// Does not touch DelegationBridge.
    /// </summary>
    public bool TickHeadless(ObservedState state)
    {
        if (Orchestrator.Phase == SimulationPhase.Planning)
        {
            return false;
        }

        RunExecutingTick(state, headlessOverride: true);
        return true;
    }

    private void RunExecutingTick(ObservedState state, bool headlessOverride)
    {
        Orchestrator.Tick(state);
        var simTick = (ulong)Math.Max(0, (long)state.SimTime);

        if (Sim.Clock.IsPaused && !headlessOverride)
        {
            SurfaceRoePolicyDeniedEngagements(state, simTick);
            return;
        }

        var executed = Orchestrator.ExecutedOrders;
        var engageOrders = new List<Order>(executed.Count);
        for (int i = 0; i < executed.Count; i++)
        {
            var o = executed[i];
            if (o.Kind == OrderKind.Engage)
            {
                engageOrders.Add(o);
            }
        }

        var commsBlocksEngage = CommsStateProjection.BlocksNewEngagement(
            CommsStateProjection.Project(Orchestrator.DecisionLog).State);
        var queued = new List<(Order Order, TargetId Victim)>();
        var deconflictSlots = new List<SwarmSalvoDeconfliction.Slot>(engageOrders.Count);
        foreach (var order in engageOrders)
        {
            var victimId = ResolveEngageVictim(order, state);
            deconflictSlots.Add(new SwarmSalvoDeconfliction.Slot(
                OrderActionMapper.TargetIdToUlong(order.Target),
                OrderActionMapper.TargetIdToUlong(victimId)));
        }

        var acceptedSlots = SwarmSalvoDeconfliction.Allocate(deconflictSlots);
        var acceptedPairs = new HashSet<(ulong Shooter, ulong Target)>(acceptedSlots.Count);
        foreach (var s in acceptedSlots)
        {
            acceptedPairs.Add((s.ShooterUnitId, s.TargetId));
        }

        foreach (var order in engageOrders)
        {
            if (commsBlocksEngage)
            {
                Orchestrator.DecisionLog.AppendPolicyDenial(new PolicyDenialRecord(
                    0,
                    state.SimTime,
                    simTick,
                    new AgentId("comms-guard"),
                    order.Target,
                    0,
                    FireAbortReason.CommsDenied,
                    OrderKind.Engage));
                continue;
            }

            var victim = ResolveEngageVictim(order, state);
            var shooterId = OrderActionMapper.TargetIdToUlong(order.Target);
            var targetId = OrderActionMapper.TargetIdToUlong(victim);
            if (!acceptedPairs.Contains((shooterId, targetId)))
            {
                continue;
            }

            var request = new EngageRequest(
                OrderActionMapper.TargetIdToUlong(order.Target),
                OrderActionMapper.TargetIdToUlong(victim),
                MountId: 0,
                SimTick: simTick);
            PrimeEngageWorld(request, state, order.Target.Value);
            Sim.EnqueueEngagement(request);
            queued.Add((order, victim));
        }

        var mode = headlessOverride ? TimeCompressionMode.HeadlessBatch : TimeCompressionMode.RealTime;
        Sim.TickOnce(mode);
        LogEngagementResults(state, queued);
        SurfaceRoePolicyDeniedEngagements(state, simTick);
        ApplyCatalogDamageHotTick(state, queued);

        var extraSteps = Math.Max(0, Sim.Clock.AccelerationFactor - 1);
        for (var i = 0; i < extraSteps; i++)
        {
            Sim.TickOnce(mode);
        }

        AdvanceLogisticsFsms(state);
    }

    private void AdvanceLogisticsFsms(ObservedState state)
    {
        ProcessLogisticsOrders(Orchestrator.ExecutedOrders, state);
        AirOps?.TickAll(1);
        BoatOps?.TickAll(1);
    }

    private void ProcessLogisticsOrders(IReadOnlyList<Order> executed, ObservedState state)
    {
        _ = state;
        if (executed.Count == 0)
        {
            return;
        }

        for (var i = 0; i < executed.Count; i++)
        {
            var order = executed[i];
            switch (order.Kind)
            {
                case OrderKind.LaunchAircraft:
                    EnsureAirOpsMap();
                    EnsureAirUnit(order.Target.Value);
                    AirOps!.TryLaunch(order.Target.Value);
                    break;
                case OrderKind.AbortLaunchAircraft:
                    EnsureAirOpsMap();
                    EnsureAirUnit(order.Target.Value);
                    AirOps!.TryAbort(order.Target.Value);
                    break;
                case OrderKind.LaunchBoat:
                    EnsureBoatOpsMap();
                    EnsureBoatCraft(order.Target.Value);
                    BoatOps!.TryLaunch(order.Target.Value);
                    break;
                case OrderKind.RecoverBoat:
                    EnsureBoatOpsMap();
                    EnsureBoatCraft(order.Target.Value);
                    BoatOps!.TryRecover(order.Target.Value);
                    break;
                case OrderKind.AbortBoatLaunch:
                    EnsureBoatOpsMap();
                    EnsureBoatCraft(order.Target.Value);
                    BoatOps!.TryAbort(order.Target.Value);
                    break;
            }
        }
    }

    private void EnsureAirOpsMap() => AirOps ??= new AirOpsStateMap();

    private void EnsureBoatOpsMap() => BoatOps ??= new BoatOpsStateMap();

    private void EnsureAirUnit(string unitId)
    {
        if (AirOps!.TryGet(unitId, out _))
        {
            return;
        }

        var ready = UnitReadiness?.IsReadyForLaunch(unitId) ?? true;
        AirOps.Upsert(AirOpsUnitState.OnGround(unitId, readyForLaunch: ready));
    }

    private void EnsureBoatCraft(string craftId)
    {
        if (BoatOps!.TryGet(craftId, out _))
        {
            return;
        }

        BoatOps.Upsert(BoatOpsUnitState.Stowed(craftId));
    }

    private void SurfaceRoePolicyDeniedEngagements(ObservedState state, ulong simTick)
    {
        var entries = Orchestrator.DecisionLog.ChronologicalEntries();
        List<TargetId>? deniedShooters = null;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Payload is PolicyDenialRecord denial &&
                denial.SimTick == simTick &&
                denial.AttemptedKind == OrderKind.Engage &&
                denial.Reason == FireAbortReason.WeaponsTight)
            {
                (deniedShooters ??= new List<TargetId>()).Add(denial.TargetId);
            }
        }

        if (deniedShooters == null)
        {
            return;
        }

        foreach (var shooter in deniedShooters)
        {
            Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromEngagement(new EngagementRecord(
                SequenceId: 0,
                state.SimTime,
                simTick,
                shooter,
                EngagementId: 0,
                Launched: false,
                EngagementAbortReasonCodes.ToLogCode(EngagementAbortReason.WeaponsTight))));
        }
    }

    private void ApplyCatalogDamageHotTick(
        ObservedState state,
        IReadOnlyList<(Order Order, TargetId Victim)> queued)
    {
        if (CatalogDamageHotTickTracker == null || CatalogReader == null)
        {
            return;
        }

        var simTick = (ulong)Math.Max(0, (long)state.SimTime);
        var outcomes = new List<CatalogDamageHotTickApplier.OutcomeApply>(queued.Count);
        var results = Sim.LastEngagementResults;
        for (var i = 0; i < queued.Count; i++)
        {
            if (i >= results.Count || !results[i].Launched || results[i].OutcomeCode == null)
            {
                continue;
            }

            var (order, victim) = queued[i];
            outcomes.Add(new CatalogDamageHotTickApplier.OutcomeApply(
                victim.Value,
                results[i].EngagementId,
                simTick,
                results[i].OutcomeCode!,
                results[i].OutcomeCode == EngagementOutcomeCodes.Hit
                    ? CombatDamageLevel.DefaultHitSeverity
                    : 0.0));
        }

        var tickResult = CatalogDamageHotTickTracker.ApplyTick(simTick, state.SimTime, outcomes);
        foreach (var change in tickResult.Changes)
        {
            Orchestrator.DecisionLog.AppendPlatformDamageChange(change);
        }

        BindCatalogWithdrawTrials(tickResult.WithdrawTrials);
        ApplyBdaContactLifecycleHotTick(simTick, tickResult.Changes);
    }

    private void ApplyBdaContactLifecycleHotTick(
        ulong simTick,
        IReadOnlyList<PlatformDamageChangeRecord> changes)
    {
        if (BdaContactLifecycleRegistry == null || changes.Count == 0)
        {
            return;
        }

        var combatDomainsEnabled = Orchestrator.ScenarioPolicy?.EngageDefaults?.CombatDomainsEnabled ?? false;
        if (!BdaContactLifecycleHotTickApplier.IsEnabled(combatDomainsEnabled))
        {
            return;
        }

        var applies = new List<BdaContactLifecycleHotTickApplier.DamageLifecycleApply>(changes.Count);
        foreach (var change in changes)
        {
            applies.Add(new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
                change.UnitId.Value,
                change.DamageLevel,
                change.NewHpPct,
                change.ReasonCode));
        }

        foreach (var targetId in BdaContactLifecycleHotTickApplier.ResolveSortedLostTargets(applies))
        {
            BdaContactLifecycleRegistry.MarkLost(targetId);
            // S116: own-side BDA loss → watch attention (hostile losses stay silent here).
            ReportOwnSideLoss(targetId, simTick, "bda:lost");
        }
    }

    private void LogEngagementResults(ObservedState state, IReadOnlyList<(Order Order, TargetId Victim)> queued)
    {
        var simTick = (ulong)Math.Max(0, (long)state.SimTime);
        var results = Sim.LastEngagementResults;
        var processed = Sim.LastProcessedEngagements;
        for (var i = 0; i < queued.Count; i++)
        {
            var (order, victim) = queued[i];
            if (i < results.Count)
            {
                var result = results[i];
                var code = result.Launched
                    ? EngagementAbortReasonCodes.Launched
                    : EngagementAbortReasonCodes.ToLogCode(result.AbortReason);
                Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromEngagement(new EngagementRecord(
                    SequenceId: 0,
                    state.SimTime,
                    simTick,
                    order.Target,
                    result.EngagementId,
                    result.Launched,
                    code)));

                if (result.Launched)
                {
                    var salvoSize = 1;
                    if (EngageWorld != null && i < processed.Count &&
                        EngageWorld.TryGetContext(processed[i], out var ctx))
                    {
                        salvoSize = Math.Max(1, ctx.SalvoSize);
                    }

                    Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromMagazineChange(new MagazineChangeRecord(
                        SequenceId: 0,
                        state.SimTime,
                        simTick,
                        order.Target,
                        MountId: 0,
                        Delta: -salvoSize,
                        MagazineChangeReasonCodes.Fire)));

                    MaybeEmitOrdnanceStateChange(state, simTick, order.Target, mountId: 0);

                    if (result.OutcomeCode != null)
                    {
                        Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromEngagementOutcome(new EngagementOutcomeRecord(
                            SequenceId: 0,
                            state.SimTime,
                            simTick,
                            order.Target,
                            victim,
                            result.EngagementId,
                            result.OutcomeCode,
                            result.PkDraw)));
                    }

                    if (result.OutcomeCode == EngagementOutcomeCodes.Kill &&
                        i < processed.Count &&
                        KilledTargets != null)
                    {
                        KilledTargets.MarkKilled(processed[i].TargetId, victim.Value);
                    }

                    BalanceDriftConsumer?.RecordEngagementOutcome(order.Target.Value, result.OutcomeCode);
                }
            }
            else
            {
                Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromEngagement(new EngagementRecord(
                    SequenceId: 0,
                    state.SimTime,
                    simTick,
                    order.Target,
                    EngagementId: 0,
                    Launched: false,
                    EngagementAbortReasonCodes.NoResult)));
            }
        }
    }

    public static SimulationSession CreateWithMvpEngagement(int globalSeed) =>
        CreateWithMvpEngagement(
            globalSeed,
            CatalogEngageEnvelope.Apply(
                new EngageContext(
                    50_000,
                    new WeaponEnvelope(1_000, 100_000),
                    RoundsRemaining: 2,
                    HasFireControlTrack: true),
                InMemoryCatalogReader.BalticPatrolFixture()),
            defaultMagazineRounds: 2);

    public static SimulationSession CreateWithMvpEngagement(
        int globalSeed,
        EngageContext defaultEngageContext,
        int defaultMagazineRounds = 2,
        IPolicyEvaluator? policyEvaluator = null)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed, policyEvaluator);
        return BindMvpEngagement(orchestrator, defaultEngageContext, defaultMagazineRounds);
    }

    public DictionaryEngageWorldQuery? EngageWorld { get; init; }

    public FuelTimelineTracker? FuelTimeline { get; set; }

    public MagazineLedger? Magazines { get; init; }

    private readonly Dictionary<string, string> _lastOrdnanceBand = new(StringComparer.Ordinal);

    public KilledTargetRegistry? KilledTargets { get; init; }

    public MvpEngagementResolver? MvpResolver { get; init; }

    public EngageContext? DefaultEngageContext { get; init; }

    public int? DefaultMagazineRounds { get; init; }

    public ICatalogReader? CatalogReader { get; init; }

    public UnitReadinessMap? UnitReadiness { get; set; }

    public AirOpsStateMap? AirOps { get; set; }

    public BoatOpsStateMap? BoatOps { get; set; }

    public IReadOnlyList<ScenarioWithdrawReadinessTrial> CatalogWithdrawTrials { get; private set; } =
        Array.Empty<ScenarioWithdrawReadinessTrial>();

    public CatalogDamageHotTickTracker? CatalogDamageHotTickTracker { get; init; }

    public BdaContactLifecycleRegistry? BdaContactLifecycleRegistry { get; init; }

    public BalanceDriftAdvisoryConsumer? BalanceDriftConsumer { get; init; }

    public void BindCatalogWithdrawTrials(IReadOnlyList<ScenarioWithdrawReadinessTrial> trials) =>
        CatalogWithdrawTrials = trials;

    public int? NextEngageSalvoOverride { get; set; }

    public Func<string, ulong, bool>? IsContactSpoofed { get; set; }

    private void PrimeEngageWorld(in EngageRequest request, ObservedState state, string shooterUnitId)
    {
        if (EngageWorld == null)
        {
            return;
        }

        if (DefaultEngageContext is { } template)
        {
            var airReady = UnitReadiness?.IsReadyForLaunch(shooterUnitId) ?? true;
            if (CatalogReader != null)
            {
                var mobilityReady = PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness(
                    shooterUnitId, CatalogReader);
                airReady = airReady && mobilityReady.ReadyForLaunch;
            }

            var radarActive = state.RadarEmconActive;
            if (CatalogReader != null)
            {
                var emconState = ScenarioEmconResolver.ResolveRadar(
                    shooterUnitId,
                    Orchestrator.ScenarioPolicy?.UnitRadarEmcon,
                    CatalogReader);
                radarActive = radarActive && emconState == EmconState.Active;
            }

            var damageWithdrawBlocked = CatalogDamageWithdrawEngageGate.BlocksEngage(
                shooterUnitId, CatalogWithdrawTrials);
            var victimId = state.PrimaryHostileContactId?.Value;
            var simTick = (ulong)Math.Max(0, (long)state.SimTime);
            var spoofed = IsContactSpoofed?.Invoke(victimId ?? "", simTick) ?? false;
            var salvo = NextEngageSalvoOverride ?? template.SalvoSize;
            NextEngageSalvoOverride = null;
            var bingoBlocked = FuelTimeline?.IsBingo(shooterUnitId) ?? false;
            var shotgunThreshold = Orchestrator.ScenarioPolicy?.EngageDefaults?.ShotgunRoundsThreshold
                ?? template.ShotgunRoundsThreshold;
            var primed = template with
            {
                HasFireControlTrack = state.HasFireControlTrack,
                RadarEmconActive = radarActive,
                AirOperationsReady = airReady,
                CatalogDamageWithdrawBlocked = damageWithdrawBlocked,
                LogisticsBingoBlocked = bingoBlocked,
                TrackSpoofed = spoofed,
                SalvoSize = Math.Max(1, salvo),
                ShotgunRoundsThreshold = Math.Max(0, shotgunThreshold),
            };
            EngageWorld.Set(request, primed);
        }
        else if (!EngageWorld.TryGetContext(request, out _))
        {
            return;
        }

        if (Magazines != null)
        {
            var fallbackRounds = DefaultMagazineRounds ?? 0;
            CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
                Magazines,
                CatalogReader,
                shooterUnitId,
                request.ShooterUnitId,
                request.MountId,
                fallbackRounds,
                out _);

            if (DefaultMagazineRounds is int policyRounds && policyRounds > 0)
            {
                var have = Magazines.GetRounds(request.ShooterUnitId, request.MountId);
                if (have > policyRounds)
                {
                    Magazines.SetRounds(request.ShooterUnitId, request.MountId, policyRounds);
                }
            }
        }
    }

    private void MaybeEmitOrdnanceStateChange(ObservedState state, ulong simTick, TargetId shooter, ulong mountId)
    {
        if (Magazines == null)
        {
            return;
        }

        var shooterUlong = OrderActionMapper.TargetIdToUlong(shooter);
        var remaining = Magazines.GetRounds(shooterUlong, mountId);
        var threshold = Orchestrator.ScenarioPolicy?.EngageDefaults?.ShotgunRoundsThreshold ?? 1;
        var band = OrdnanceStateBands.Resolve(remaining, threshold);
        var unitKey = shooter.Value;
        if (!_lastOrdnanceBand.TryGetValue(unitKey, out var previous))
        {
            previous = OrdnanceStateBands.Nominal;
            if (band == OrdnanceStateBands.Nominal)
            {
                _lastOrdnanceBand[unitKey] = band;
                return;
            }
        }

        if (previous == band)
        {
            return;
        }

        _lastOrdnanceBand[unitKey] = band;
        Orchestrator.OrderLog.Append(OrderLogEntryFactories.FromOrdnanceStateChange(new OrdnanceStateChangeRecord(
            SequenceId: 0,
            state.SimTime,
            simTick,
            shooter,
            previous,
            band,
            remaining)));
    }

    private static TargetId ResolveEngageVictim(Order order, ObservedState state)
    {
        if (BalticV3SideRegistry.IsRedForceUnit(order.Target.Value))
        {
            var blue = state.PrimaryBlueForceContactId
                ?? (BalticV3SideRegistry.GetDefaultBlueUnitId() is { } bid
                    ? new TargetId(bid)
                    : new TargetId("u1"));
            return blue;
        }

        if (state.PreferredHostileByShooter != null
            && state.PreferredHostileByShooter.TryGetValue(order.Target.Value, out var preferred)
            && !string.IsNullOrWhiteSpace(preferred))
        {
            return new TargetId(preferred);
        }

        return state.PrimaryHostileContactId
            ?? (BalticV3SideRegistry.GetDefaultRedUnitId() is { } rid
                ? new TargetId(rid)
                : new TargetId("hostile-1"));
    }
}
