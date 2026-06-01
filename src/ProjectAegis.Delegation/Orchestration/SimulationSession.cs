namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
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
        var sim = new SimTickPipeline(seed, new MvpEngagementResolver(world, magazines));
        return new SimulationSession(orchestrator, sim)
        {
            EngageWorld = world,
            Magazines = magazines,
            DefaultEngageContext = defaultEngageContext,
            DefaultMagazineRounds = defaultMagazineRounds,
        };
    }

    /// <summary>Bind MVP engage using scenario policy JSON engage defaults when present.</summary>
    public static SimulationSession BindMvpEngagementForScenario(
        DelegationOrchestrator orchestrator,
        string? scenarioPolicyId)
    {
        var profile = string.IsNullOrWhiteSpace(scenarioPolicyId)
            ? null
            : ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        var engage = profile?.ResolveEngageContext()
            ?? ScenarioEngageDefaults.MvpFallback.ToEngageContext(
                ScenarioEngageDefaults.MvpFallback.DefaultMagazineRounds);
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
        foreach (var order in engageOrders)
        {
            var request = new EngageRequest(
                OrderActionMapper.TargetIdToUlong(order.Target),
                TargetId: 0,
                MountId: 0,
                SimTick: simTick);
            PrimeEngageWorld(request, state);
            Sim.EnqueueEngagement(request);
        }

        Sim.TickOnce(TimeCompressionMode.RealTime);
        LogEngagementResults(state, engageOrders);
    }

    private void LogEngagementResults(ObservedState state, IReadOnlyList<Order> engageOrders)
    {
        var simTick = (ulong)Math.Max(0, (long)state.SimTime);
        var results = Sim.LastEngagementResults;
        for (var i = 0; i < engageOrders.Count; i++)
        {
            if (i < results.Count)
            {
                var result = results[i];
                var code = result.Launched
                    ? EngagementAbortReasonCodes.Launched
                    : EngagementAbortReasonCodes.ToLogCode(result.AbortReason);
                Orchestrator.DecisionLog.AppendEngagement(new EngagementRecord(
                    SequenceId: 0,
                    state.SimTime,
                    simTick,
                    engageOrders[i].Target,
                    result.EngagementId,
                    result.Launched,
                    code));

                if (result.Launched)
                {
                    Orchestrator.DecisionLog.AppendMagazineChange(new MagazineChangeRecord(
                        SequenceId: 0,
                        state.SimTime,
                        simTick,
                        engageOrders[i].Target,
                        MountId: 0,
                        Delta: -1,
                        MagazineChangeReasonCodes.Fire));
                }
            }
            else
            {
                Orchestrator.DecisionLog.AppendEngagement(new EngagementRecord(
                    SequenceId: 0,
                    state.SimTime,
                    simTick,
                    engageOrders[i].Target,
                    EngagementId: 0,
                    Launched: false,
                    EngagementAbortReasonCodes.NoResult));
            }
        }
    }

    public static SimulationSession CreateWithMvpEngagement(int globalSeed) =>
        CreateWithMvpEngagement(
            globalSeed,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 2,
                HasFireControlTrack: true),
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

    public EngageContext? DefaultEngageContext { get; init; }

    public int? DefaultMagazineRounds { get; init; }

    private void PrimeEngageWorld(in EngageRequest request, ObservedState state)
    {
        if (EngageWorld == null)
        {
            return;
        }

        if (DefaultEngageContext is { } template)
        {
            var primed = template with { HasFireControlTrack = state.HasFireControlTrack };
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
