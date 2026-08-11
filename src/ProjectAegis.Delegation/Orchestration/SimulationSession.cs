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

    /// <summary>Attach MVP engage pipeline to an existing orchestrator (Unity bridge, shared session).</summary>
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

    /// <summary>Bind MVP engage using scenario policy JSON engage defaults when present.</summary>
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

    /// <summary>S115: session-local watch attention queue (pure; no Bridge).</summary>
    public WatchAttentionQueue WatchQueue { get; } = new();

    /// <summary>S115: auto-pause / resume gate for pause-class cards.</summary>
    public WatchAutoPauseGate WatchPauseGate { get; } = new();

    /// <summary>Most recent auto-pause reason from the watch gate.</summary>
    public WatchPauseReason LastWatchPauseReason => WatchPauseGate.LastPauseReason;

    /// <summary>Whether the sim clock is paused (interactive modes skip advance). DRG-14 / S112-02.</summary>
    public bool IsSimPaused => Sim.Clock.IsPaused;

    /// <summary>Pause sim time advance on the session tick path. Distinct from agent/order pause.</summary>
    public void PauseSim() => Sim.Clock.Pause();

    /// <summary>Resume sim time advance after <see cref="PauseSim"/>. Does not check watch queue.</summary>
    public void ResumeSim() => Sim.Clock.Resume();

    /// <summary>
    /// S115-03: resume only when zero unresolved pause-class cards, or when
    /// <paramref name="explicitOverride"/> is true (player force-resume).
    /// Clears the stored watch pause reason on success.
    /// </summary>
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

    /// <summary>
    /// S115: enqueue a watch attention event. If it is pause-class, auto-pauses the sim
    /// via the existing <see cref="PauseSim"/> path and records the reason.
    /// Idempotent on EventId. Detection / BDA emitters call this with pure facts only.
    /// </summary>
    public void ReportWatchAttention(WatchAttentionEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        WatchQueue.Enqueue(evt);
        if (WatchPauseGate.ShouldAutoPause(evt))
        {
            PauseSim();
        }
    }

    /// <summary>Steps per Accelerated session tick (1..256). DRG-14 / S112-02.</summary>
    public int TimeAccelerationFactor => Sim.Clock.AccelerationFactor;

    /// <summary>Sets acceleration factor (clamped 1..256) used when > 1 on the session tick path.</summary>
    public void SetTimeAccelerationFactor(int factor) => Sim.Clock.SetAccelerationFactor(factor);

    public void BeginExecution() => Orchestrator.BeginExecution();

    public bool Tick(ObservedState state)
    {
        if (Orchestrator.Phase == SimulationPhase.Planning)
        {
            return false;
        }

        RunExecutingTick(state);
        return true;
    }

    private void RunExecutingTick(ObservedState state)
    {
        Orchestrator.Tick(state);
        var simTick = (ulong)Math.Max(0, (long)state.SimTime);

        // S112-02 / DRG-14: when paused, freeze the sim engagement pipeline and
        // consumers that depend on TickOnce. Do not enqueue (would strand pending)
        // or consume LastEngagementResults (stale/misaligned with this tick's orders).
        if (Sim.Clock.IsPaused)
        {
            SurfaceRoePolicyDeniedEngagements(state, simTick);
            return;
        }

        // Allocation follow-up P1: explicit loop instead of LINQ Where+ToArray per tick.
        // Uses List<Order> (Count/foreach compatible) to avoid per-tick enumerator + array alloc.
        // Behavior and iteration order identical (ExecutedOrders order preserved for engages).
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
        // P2 allocation follow-up (S37-09): explicit loop instead of Select LINQ for HashSet population in hot engage path.
        // Avoids per-tick iterator allocation while preserving identical membership and determinism.
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

        // S112-02 / DRG-14: process the first step as RealTime so queued engagements resolve
        // into LastEngagementResults for session consumers. Extra steps implement acceleration
        // (equivalent to TickOnce(Accelerated) after the pending queue is drained — Lane A
        // TC-CLK-3). A single TickOnce(Accelerated) would clear results on later substeps.
        Sim.TickOnce(TimeCompressionMode.RealTime);
        LogEngagementResults(state, queued);
        SurfaceRoePolicyDeniedEngagements(state, simTick);
        ApplyCatalogDamageHotTick(state, queued);

        var extraSteps = Math.Max(0, Sim.Clock.AccelerationFactor - 1);
        for (var i = 0; i < extraSteps; i++)
        {
            Sim.TickOnce(TimeCompressionMode.RealTime);
        }

        AdvanceLogisticsFsms(state);
    }

    /// <summary>
    /// LOG-08 / LOG-09: apply executed air/boat logistics orders then advance FSM timers once per session tick.
    /// Maps are optional — only ticked when non-null; auto-created on first logistics order.
    /// </summary>
    private void AdvanceLogisticsFsms(ObservedState state)
    {
        ProcessLogisticsOrders(Orchestrator.ExecutedOrders, state);
        AirOps?.TickAll(1);
        BoatOps?.TickAll(1);
    }

    /// <summary>
    /// Apply Launch/Abort air and Launch/Recover/Abort boat orders from this tick's executed list.
    /// Missing unit/craft rows are upserted from readiness (air) or stowed defaults (boat).
    /// </summary>
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

    /// <summary>
    /// Policy–engage unification (epic policy-engage-unification-slice): an engage intent denied by ROE
    /// (<see cref="FireAbortReason.WeaponsTight"/>) is rejected at the agent/policy layer before it reaches
    /// <c>MvpEngagementResolver</c>, so it never produces an engagement-abort row on its own. Surface each such
    /// denial for this tick as an <c>Engagement|…|ROE_WEAPONS_TIGHT</c> abort row so the harness fingerprint carries
    /// the canonical abort code, mirroring the resolver-side mapping in <c>MvpEngagementResolver.MapPolicyDenial</c>.
    /// The original <c>PolicyDenial</c> row is preserved; scoping to WeaponsTight leaves the comms-guard denial
    /// (<see cref="FireAbortReason.CommsDenied"/>) and all other paths untouched.
    /// </summary>
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
        ApplyBdaContactLifecycleHotTick(tickResult.Changes);
    }

    private void ApplyBdaContactLifecycleHotTick(IReadOnlyList<PlatformDamageChangeRecord> changes)
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

        // Allocation follow-up P1: explicit foreach + List instead of Select+ToArray.
        // DTOs still allocated (small records) but no LINQ iterator chain per tick.
        // Same inputs to ResolveSortedLostTargets; determinism preserved.
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

    /// <summary>Optional fuel burn tracker for Bingo engage gate (logistics v1).</summary>
    public FuelTimelineTracker? FuelTimeline { get; set; }

    public MagazineLedger? Magazines { get; init; }

    private readonly Dictionary<string, string> _lastOrdnanceBand = new(StringComparer.Ordinal);

    public KilledTargetRegistry? KilledTargets { get; init; }

    public MvpEngagementResolver? MvpResolver { get; init; }

    public EngageContext? DefaultEngageContext { get; init; }

    public int? DefaultMagazineRounds { get; init; }

    /// <summary>ADR-006 read path for live magazine counts (Req-16).</summary>
    public ICatalogReader? CatalogReader { get; init; }

    public UnitReadinessMap? UnitReadiness { get; set; }

    /// <summary>LOG-08 air-ops FSM ledger (optional; advanced on session tick).</summary>
    public AirOpsStateMap? AirOps { get; set; }

    /// <summary>LOG-09…11 boat-ops FSM ledger (optional; advanced on session tick).</summary>
    public BoatOpsStateMap? BoatOps { get; set; }

    /// <summary>Catalog-resolved withdraw/readiness trials (refreshed by hot-tick applier when enabled).</summary>
    public IReadOnlyList<ScenarioWithdrawReadinessTrial> CatalogWithdrawTrials { get; private set; } =
        Array.Empty<ScenarioWithdrawReadinessTrial>();

    /// <summary>Bounded catalog hot-tick damage tracker (combatDomainsEnabled + catalogWithdraw only).</summary>
    public CatalogDamageHotTickTracker? CatalogDamageHotTickTracker { get; init; }

    /// <summary>Pending BDA Lost promotions drained by replay harness after each tick (S32-09).</summary>
    public BdaContactLifecycleRegistry? BdaContactLifecycleRegistry { get; init; }

    /// <summary>Advisory-only balance drift telemetry consumer (DBI-5; default disabled).</summary>
    public BalanceDriftAdvisoryConsumer? BalanceDriftConsumer { get; init; }

    public void BindCatalogWithdrawTrials(IReadOnlyList<ScenarioWithdrawReadinessTrial> trials) =>
        CatalogWithdrawTrials = trials;

    /// <summary>Salvo size for the next primed engage (interactive attack menu).</summary>
    public int? NextEngageSalvoOverride { get; set; }

    /// <summary>Returns whether the contact id is under active spoof at the given tick.</summary>
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
                    shooterUnitId,
                    CatalogReader);
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
                shooterUnitId,
                CatalogWithdrawTrials);
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

            // Scenario policy DefaultMagazineRounds is an authoritative engagement budget.
            // Cap catalog-seeded totals so magazine-depletion scenarios (e.g. baltic-patrol-magazine)
            // still emit NO_AMMO when the policy specifies a tight magazine.
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
        // order.Target is the shooter unit id. Red shooters engage blue force;
        // blue (and unknown) shooters engage preferred or primary hostile contact.
        if (BalticV3SideRegistry.IsRedForceUnit(order.Target.Value))
        {
            var blue = state.PrimaryBlueForceContactId
                ?? (BalticV3SideRegistry.GetDefaultBlueUnitId() is { } bid
                    ? new TargetId(bid)
                    : new TargetId("u1"));
            return blue;
        }

        // Multi-domain: each shooter uses its detection-paired red when present.
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
