namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Time;

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
        int defaultMagazineRounds = 2)
    {
        var seed = SimSeed.FromScenario((ulong)orchestrator.GlobalSeed);
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        var killedTargets = new KilledTargetRegistry();
        var speculative = orchestrator.ScenarioPolicy?.Speculative
            ?? ScenarioSpeculativeSettings.CampaignDefault;
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            orchestrator.PolicyEvaluator,
            orchestrator.ResolveEffectivePolicyForUnit,
            seed,
            killedTargets,
            speculative);
        var sim = new SimTickPipeline(seed, resolver);
        return new SimulationSession(orchestrator, sim)
        {
            EngageWorld = world,
            Magazines = magazines,
            KilledTargets = killedTargets,
            MvpResolver = resolver,
            DefaultEngageContext = defaultEngageContext,
            DefaultMagazineRounds = defaultMagazineRounds,
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
        return BindMvpEngagement(orchestrator, engage, rounds);
    }

    public SimulationPhase Phase => Orchestrator.Phase;

    public bool AttachReplayViewer
    {
        get => Orchestrator.AttachReplayViewer;
        set => Orchestrator.AttachReplayViewer = value;
    }

    public DelegationOrchestrator Orchestrator { get; }

    public SimTickPipeline Sim { get; }

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
        var engageOrders = Orchestrator.ExecutedOrders
            .Where(o => o.Kind == OrderKind.Engage)
            .ToArray();

        var simTick = (ulong)Math.Max(0, (long)state.SimTime);
        var commsBlocksEngage = CommsStateProjection.BlocksNewEngagement(
            CommsStateProjection.Project(Orchestrator.DecisionLog).State);
        var queued = new List<(Order Order, TargetId Victim)>();
        var deconflictSlots = new List<SwarmSalvoDeconfliction.Slot>(engageOrders.Length);
        foreach (var order in engageOrders)
        {
            var victimId = state.PrimaryHostileContactId ?? new TargetId("hostile-1");
            deconflictSlots.Add(new SwarmSalvoDeconfliction.Slot(
                OrderActionMapper.TargetIdToUlong(order.Target),
                OrderActionMapper.TargetIdToUlong(victimId)));
        }

        var acceptedSlots = SwarmSalvoDeconfliction.Allocate(deconflictSlots);
        var acceptedPairs = new HashSet<(ulong Shooter, ulong Target)>(
            acceptedSlots.Select(s => (s.ShooterUnitId, s.TargetId)));

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

            var victim = state.PrimaryHostileContactId ?? new TargetId("hostile-1");
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

        Sim.TickOnce(TimeCompressionMode.RealTime);
        LogEngagementResults(state, queued);
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

    public MagazineLedger? Magazines { get; init; }

    public KilledTargetRegistry? KilledTargets { get; init; }

    public MvpEngagementResolver? MvpResolver { get; init; }

    public EngageContext? DefaultEngageContext { get; init; }

    public int? DefaultMagazineRounds { get; init; }

    public UnitReadinessMap? UnitReadiness { get; set; }

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
            var victimId = state.PrimaryHostileContactId?.Value;
            var simTick = (ulong)Math.Max(0, (long)state.SimTime);
            var spoofed = IsContactSpoofed?.Invoke(victimId ?? "", simTick) ?? false;
            var salvo = NextEngageSalvoOverride ?? template.SalvoSize;
            NextEngageSalvoOverride = null;
            var primed = template with
            {
                HasFireControlTrack = state.HasFireControlTrack,
                RadarEmconActive = state.RadarEmconActive,
                AirOperationsReady = airReady,
                TrackSpoofed = spoofed,
                SalvoSize = Math.Max(1, salvo),
            };
            EngageWorld.Set(request, primed);
        }
        else if (!EngageWorld.TryGetContext(request, out _))
        {
            return;
        }

        if (Magazines != null && DefaultMagazineRounds is int defaultRounds && defaultRounds > 0)
        {
            Magazines.EnsureInitialRounds(request.ShooterUnitId, request.MountId, defaultRounds);
        }
    }
}
