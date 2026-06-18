namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;

using ProjectAegis.Delegation.Mission;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Replay;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

/// <summary>Headless Baltic slice runner for CLI and replay-verify gate.</summary>
public static class BalticReplayHarness
{
    public sealed record Result(
        int Seed,
        string ScenarioPolicyId,
        int Ticks,
        string Fingerprint,
        string FingerprintSha256,
        int EngagementCount,
        ulong DetectionWorldHash,
        ulong WorldHash,
        IReadOnlyList<ReplayCheckpoint> Checkpoints,
        IReadOnlyList<MessageLogLine> Messages,
        SensorC2Snapshot SensorC2,
        string ScoringCsvRow);

    public static Result Run(
        int seed,
        string scenarioPolicyId,
        int ticks,
        bool mvpEngagement = true,
        ICatalogReader? catalog = null,
        IReadOnlyDictionary<string, bool>? unitReadiness = null,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits = null,
        int maxTechnologyLevel = 2)
    {
        if (ticks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), "ticks must be >= 1");
        }

        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(scenarioPolicyId);
        var catalogReader = catalog
            ?? CatalogReaderFactory.TryCreateBalticPatrolReader()
            ?? InMemoryCatalogReader.BalticPatrolFixture();
        var detectionTrials = profile == null
            ? Array.Empty<ScenarioDetectionTrial>()
            : DetectionTrialResolver.Resolve(profile, catalogReader);
        PdDetectionContactSimulator? pdSim = null;
        ScenarioContactSimulator? scheduleSim = null;
        if (detectionTrials.Count > 0 && profile != null)
        {
            pdSim = new PdDetectionContactSimulator(
                SimSeed.FromScenario((ulong)seed),
                detectionTrials,
                profile.UnitRadarEmcon,
                profile.Jammers,
                profile.ContactLifecycle);
        }
        else if (profile?.ContactSeeds.Count > 0)
        {
            scheduleSim = new ScenarioContactSimulator(profile.ContactSeeds, profile.UnitRadarEmcon);
        }

        var missionRuntime = profile != null ? MissionRuntimeFactory.TryCreate(profile.MissionTimeline) : null;
        var checkpointStore = new ReplayCheckpointStore();
        var checkpointInterval = profile?.ReplaySettings.CheckpointIntervalTicks ?? 0;

        var bridge = new DelegationBridge(
            seed,
            mvpEngagement: mvpEngagement,
            scenarioPolicyId: scenarioPolicyId,
            catalog: catalogReader);
        if (mvpEngagement && bridge.Session == null)
        {
            throw new InvalidOperationException("MVP engage session was not created.");
        }

        if (bridge.Session != null)
        {
            var readiness = unitReadiness ?? profile?.UnitReadiness;
            if (readiness is { Count: > 0 })
            {
                bridge.Session.UnitReadiness = new UnitReadinessMap(readiness);
            }

            if (profile != null)
            {
                bridge.Session.BindCatalogWithdrawTrials(
                    ReadinessPolicyEvaluator.ResolveCatalogTrials(profile, catalogReader));
            }
        }

        var unitBinding = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        RegisterNearFutureUnits(bridge, nearFutureUnits, maxTechnologyLevel);
        var unit = unitBinding.Target;
        var agentPolicy = profile?.DelegationSettings.UsePatrolCandidates == true
            ? (IPolicy)new PatrolCandidateEngagePolicy()
            : new EngageOnlyPolicy();
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: agentPolicy);
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.BeginExecution();

        var harness = new HeadlessSnapshot(
            pdSim,
            scheduleSim,
            profile?.UnitRadarEmcon,
            bridge.Session?.KilledTargets,
            fallbackContactCount: 2,
            fallbackHasTrack: true);
        for (var t = 0; t < ticks; t++)
        {
            harness.Advance(1.0);
            var simTick = (ulong)Math.Max(0, (long)harness.SimTime);

            if (missionRuntime != null)
            {
                foreach (var emission in missionRuntime.Tick(simTick, harness.SimTime, 0))
                {
                    switch (emission.Event.Kind)
                    {
                        case MissionEventKind.MissionTransition:
                            bridge.Orchestrator.OrderLog.Append(
                                OrderLogEntryFactories.FromMissionTransition(
                                    new MissionTransitionRecord(
                                        emission.SequenceId,
                                        emission.SimTime,
                                        emission.SimTick,
                                        emission.Event.EventId,
                                        emission.Event.Code)));
                            break;
                        case MissionEventKind.EventFired:
                            bridge.Orchestrator.OrderLog.Append(
                                OrderLogEntryFactories.FromEventFired(
                                    new EventFiredRecord(
                                        emission.SequenceId,
                                        emission.SimTime,
                                        emission.SimTick,
                                        emission.Event.EventId,
                                        emission.Event.Code)));
                            break;
                    }
                }
            }

            if (pdSim != null && profile != null)
            {
                pdSim.SetCommsStaleThresholdDivisor(
                    CommsTrackStaleness.StaleThresholdDivisor(bridge.CurrentCommsState, profile.CommsDisplay));
            }

            var transitions = pdSim != null
                ? pdSim.Tick(simTick, harness.SimTime)
                : scheduleSim?.Tick(simTick, harness.SimTime) ?? Array.Empty<ContactTransition>();
            foreach (var transition in transitions)
            {
                bridge.Orchestrator.OrderLog.AppendContactTransition(transition);
            }

            bridge.Tick(harness, harness);

            if (bridge.Session?.KilledTargets is { } killed && pdSim != null)
            {
                foreach (var (_, label) in killed.DrainNewKills())
                {
                    foreach (var killTransition in pdSim.ApplyTargetKill(simTick, harness.SimTime, label))
                    {
                        bridge.Orchestrator.OrderLog.AppendContactTransition(killTransition);
                    }
                }
            }

            if (checkpointInterval > 0 && simTick % (ulong)checkpointInterval == 0)
            {
                var simHashTick = bridge.Session?.Sim.LastWorldHash ?? 0;
                var detectionHashTick = pdSim?.LastDetectionHash ?? 0;
                var worldHashTick = SimWorldHash.Combine(simHashTick, detectionHashTick, 0);
                checkpointStore.Record(
                    simTick,
                    worldHashTick,
                    bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
                    bridge.Orchestrator.DecisionLog.ChronologicalEntries().LastOrDefault()?.SequenceId ?? 0);
            }
        }

        var simHash = bridge.Session?.Sim.LastWorldHash ?? 0;
        var detectionHash = pdSim?.LastDetectionHash ?? 0;
        var worldHash = SimWorldHash.Combine(simHash, detectionHash, 0);

        var messages = MessageLogProjection.Project(bridge.Orchestrator.DecisionLog);
        var sensorC2 = SensorC2Bridge.Build(harness, bridge.Orchestrator.DecisionLog);
        var scoringCsv = LossesScoringCsvExporter.FormatRow(
            scenarioPolicyId,
            seed,
            "BLUE",
            bridge.Orchestrator.DecisionLog);
        var fingerprint = bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        return new Result(
            seed,
            scenarioPolicyId,
            ticks,
            fingerprint,
            OrderLogReplayFingerprint.ComputeSha256Hex(bridge.Orchestrator.DecisionLog),
            bridge.Orchestrator.DecisionLog.Engagements.Count,
            detectionHash,
            worldHash,
            checkpointStore.Checkpoints,
            messages,
            sensorC2,
            scoringCsv);
    }

    private sealed class HeadlessSnapshot : ISimWorldSnapshot, IOrderSink
    {
        private readonly PdDetectionContactSimulator? _pd;
        private readonly ScenarioContactSimulator? _schedule;
        private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
        private readonly KilledTargetRegistry? _killedTargets;
        private readonly int _fallbackContactCount;
        private readonly bool _fallbackHasTrack;
        private double _simTime;

        public HeadlessSnapshot(
            PdDetectionContactSimulator? pd,
            ScenarioContactSimulator? schedule,
            IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
            KilledTargetRegistry? killedTargets,
            int fallbackContactCount,
            bool fallbackHasTrack)
        {
            _pd = pd;
            _schedule = schedule;
            _unitRadarEmcon = unitRadarEmcon;
            _killedTargets = killedTargets;
            _fallbackContactCount = fallbackContactCount;
            _fallbackHasTrack = fallbackHasTrack;
        }

        public double SimTime => _simTime;

        public int ContactCount =>
            _pd?.ActiveCount ?? _schedule?.ActiveCount ?? _fallbackContactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId
        {
            get
            {
                if (_pd?.PrimaryTargetId is { } pdId)
                {
                    return new TargetId(pdId);
                }

                if (_schedule?.PrimaryTargetId is { } schedId)
                {
                    return new TargetId(schedId);
                }

                return ContactCount > 0 ? new TargetId("hostile-1") : null;
            }
        }

        public bool HasFireControlTrackOnPrimaryContact
        {
            get
            {
                if (_pd != null && _pd.ActiveCount > 0)
                {
                    return _pd.PrimaryHasFireControlTrack;
                }

                if (_schedule != null && _schedule.ActiveCount > 0)
                {
                    return _schedule.PrimaryHasFireControlTrack;
                }

                return ContactCount > 0 && _fallbackHasTrack;
            }
        }

        public bool ObserverRadarEmconActive =>
            ScenarioEmconResolver.ResolveRadar("u1", _unitRadarEmcon) == EmconState.Active;

        public void Advance(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId)
        {
            if (_killedTargets == null)
            {
                return true;
            }

            var id = Roe.OrderActionMapper.TargetIdToUlong(memberId);
            return !_killedTargets.IsKilled(id);
        }

        public void ApplyOrder(EntityKey entity, in Order order) { }
    }

    private static void RegisterNearFutureUnits(
        DelegationBridge bridge,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits,
        int maxTechnologyLevel)
    {
        if (nearFutureUnits == null || nearFutureUnits.Count == 0)
        {
            return;
        }

        var catalogPath = ResolveNearFutureCatalogPath();
        var plans = NearFutureArchetypeRuntime.PlanSpawns(
            nearFutureUnits,
            maxTechnologyLevel,
            SwarmTier.Medium,
            catalogPath);
        var entityKey = 2;
        foreach (var plan in plans)
        {
            bridge.Registry.RegisterUnit(new EntityKey(entityKey++), plan.UnitId);
            bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(
                0,
                0,
                0,
                plan.UnitId,
                $"NF_SPAWN:{plan.ArchetypeId}"));
        }
    }

    private static string ResolveNearFutureCatalogPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("near_future_archetypes.json");
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }
}