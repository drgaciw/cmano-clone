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
using ProjectAegis.Sim.Catalog;
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
        string ScoringCsvRow,
        DecisionLog DecisionLog,
        IReadOnlyList<string> FireOrder);

    /// <summary>
    /// Resolves AC-2 <c>fire_order</c>: policy mission timeline when present, else chronological
    /// <see cref="OrderLogEntryKind.EventFired"/> event ids from the decision log.
    /// </summary>
    public static IReadOnlyList<string> ResolveFireOrder(
        ScenarioMissionTimeline? missionTimeline,
        DecisionLog decisionLog)
    {
        if (missionTimeline != null)
        {
            return missionTimeline.FireOrder;
        }

        var fired = new List<string>();
        foreach (var entry in decisionLog.ChronologicalEntries())
        {
            if (entry.Kind == OrderLogEntryKind.EventFired && entry.Payload is EventFiredRecord record)
            {
                fired.Add(record.EventId);
            }
        }

        return fired;
    }

    /// <summary>
    /// Polish helper (S36-05): reports first mismatch tick + sub-hash component for divergence diagnosis.
    /// Does not affect Run path, append order, or detection trial ordering. For test harness error paths only.
    /// </summary>
    public static string DiagnoseDivergence(Result a, Result b)
    {
        if (a.Fingerprint == b.Fingerprint && a.WorldHash == b.WorldHash && a.DetectionWorldHash == b.DetectionWorldHash)
            return "MATCH";
        // checkpoints provide tick-level localization (recorded at interval)
        for (int i = 0; i < Math.Min(a.Checkpoints.Count, b.Checkpoints.Count); i++)
        {
            var ca = a.Checkpoints[i];
            var cb = b.Checkpoints[i];
            if (ca.WorldHash != cb.WorldHash || ca.LogFingerprint != cb.LogFingerprint)
            {
                return $"First mismatch at tick {ca.SimTick}: worldHash {ca.WorldHash} vs {cb.WorldHash}; fingerprint differs; layer hint in subhash";
            }
        }
        return $"Final mismatch: WORLD={a.WorldHash}!={b.WorldHash} DET={a.DetectionWorldHash}!={b.DetectionWorldHash} FINGERPRINT differs";
    }

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
            ?? CatalogReaderFactory.ResolveForScenario(scenarioPolicyId);
        var detectionTrials = profile == null
            ? Array.Empty<ScenarioDetectionTrial>()
            : DetectionTrialResolver.Resolve(profile, catalogReader);
        PdDetectionContactSimulator? pdSim = null;
        ScenarioContactSimulator? scheduleSim = null;
        DatalinkSidePictureMerger? datalinkMerger = null;
        if (detectionTrials.Count > 0 && profile != null)
        {
            pdSim = new PdDetectionContactSimulator(
                SimSeed.FromScenario((ulong)seed),
                detectionTrials,
                profile.UnitRadarEmcon,
                profile.Jammers,
                profile.ContactLifecycle,
                catalogReader);
            if (profile.DatalinkDoctrine.IsSharingEnabled)
            {
                var datalinkDoctrine = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalogReader);
                datalinkMerger = new DatalinkSidePictureMerger(datalinkDoctrine, detectionTrials);
            }
        }
        else if (profile?.ContactSeeds.Count > 0)
        {
            scheduleSim = new ScenarioContactSimulator(profile.ContactSeeds, profile.UnitRadarEmcon);
        }

        var missionRuntime = profile != null ? MissionRuntimeFactory.TryCreate(profile.MissionTimeline) : null;
        var contactTriggerRuntime = profile != null
            ? MissionContactTriggerRuntimeFactory.TryCreate(profile.MissionTimeline)
            : null;
        var sideMaxSalvo = profile?.FriendlyDefault.MaxSalvo ?? EffectivePolicy.DefaultFree.MaxSalvo;
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
        var nextEntityKey = 2;
        SimEntityBinding? hostileBinding = null;
        if (CatalogReaderFactory.IsBalticV3Scenario(scenarioPolicyId))
        {
            bridge.Registry.RegisterUnit(new EntityKey(nextEntityKey++), "ucav-blue");
            bridge.Registry.RegisterUnit(new EntityKey(nextEntityKey++), "ucav-red");
            hostileBinding = bridge.Registry.RegisterUnit(new EntityKey(nextEntityKey++), "hostile-1");
        }

        RegisterNearFutureUnits(bridge, nearFutureUnits, maxTechnologyLevel, nextEntityKey);
        var unit = unitBinding.Target;
        var patrolPolicyFactory = profile?.DelegationSettings.UsePatrolCandidates == true
            ? (Func<EngagePrimaryMode, IPolicy>)(mode => new PatrolCandidateEngagePolicy(mode))
            : _ => new EngageOnlyPolicy();
        var bluePolicy = profile?.ResolveForUnit("u1", isFriendly: true) ?? EffectivePolicy.DefaultFree;
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: patrolPolicyFactory(EngagePrimaryMode.Hostile));
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, bluePolicy, isFriendly: true);

        if (hostileBinding != null)
        {
            var redPolicy = profile?.ResolveForUnit("hostile-1", isFriendly: false) ?? EffectivePolicy.DefaultFree;
            var redAgent = bridge.Orchestrator.CreateAgent(
                new AgentId("a-red"),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.FullAutonomous,
                policy: patrolPolicyFactory(EngagePrimaryMode.BlueForce));
            bridge.Orchestrator.AssignAgentToTarget(
                redAgent,
                hostileBinding.Target,
                redPolicy,
                isFriendly: false);
        }
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
            if (datalinkMerger != null)
            {
                var shared = datalinkMerger.Merge(
                    transitions,
                    simTick,
                    harness.SimTime,
                    MapDatalinkCommsShareState(bridge.CurrentCommsState));
                if (shared.Count > 0)
                {
                    // Allocation follow-up P1: List concat instead of LINQ Concat+ToArray.
                    // Order preserved (transitions then shared) for identical order-log appends and hash.
                    var merged = new List<ContactTransition>(transitions.Count + shared.Count);
                    merged.AddRange(transitions);
                    merged.AddRange(shared);
                    transitions = merged.ToArray();
                }
            }

            foreach (var transition in transitions)
            {
                if (contactTriggerRuntime != null)
                {
                    foreach (var emission in contactTriggerRuntime.Evaluate(transition, harness.SimTime, simTick))
                    {
                        var trigger = emission.Trigger;
                        bridge.Orchestrator.OrderLog.Append(
                            OrderLogEntryFactories.FromMissionTransition(
                                new MissionTransitionRecord(
                                    0,
                                    emission.SimTime,
                                    emission.SimTick,
                                    trigger.TriggerId,
                                    trigger.MissionCode)));
                        var isFriendly = trigger.PolicySide == MissionContactPolicySide.Friendly;
                        var roePolicy = new EffectivePolicy(trigger.Roe, sideMaxSalvo);
                        bridge.Orchestrator.ApplyRoeToUnits(
                            trigger.UnitIds,
                            roePolicy,
                            isFriendly,
                            emission.SimTime,
                            emission.SimTick);
                    }
                }

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

            if (bridge.Session?.BdaContactLifecycleRegistry is { } bdaLifecycle && pdSim != null)
            {
                foreach (var lostTransition in BdaContactLifecycleHotTickApplier.ApplyFromRegistry(
                             pdSim,
                             simTick,
                             harness.SimTime,
                             bdaLifecycle))
                {
                    bridge.Orchestrator.OrderLog.AppendContactTransition(lostTransition);
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
        var fireOrder = ResolveFireOrder(profile?.MissionTimeline, bridge.Orchestrator.DecisionLog);
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
            scoringCsv,
            bridge.Orchestrator.DecisionLog,
            fireOrder);
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

        public TargetId? PrimaryBlueForceContactId
        {
            get
            {
                string? candidate = _pd?.PrimaryBlueForceTargetId;
                if (candidate == null || !BalticV3SideRegistry.IsBlueForceUnit(candidate))
                {
                    return null;
                }

                return new TargetId(candidate);
            }
        }

        public bool PrimaryBlueForceContactDestroyed
        {
            get
            {
                var primary = PrimaryBlueForceContactId;
                if (primary is not { } p || _killedTargets == null)
                {
                    return false;
                }

                var id = Roe.OrderActionMapper.TargetIdToUlong(p);
                return _killedTargets.IsKilled(id);
            }
        }

        public TargetId? PrimaryHostileContactId
        {
            get
            {
                string? candidate = null;
                if (_pd?.PrimaryTargetId is { } pdId)
                {
                    candidate = pdId;
                }
                else if (_schedule?.PrimaryTargetId is { } schedId)
                {
                    candidate = schedId;
                }
                else if (ContactCount > 0)
                {
                    candidate = "hostile-1";
                }

                if (candidate == null || !HostileContactFilter.IsEngageableHostileTarget(candidate))
                {
                    return null;
                }

                return new TargetId(candidate);
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

        public bool PrimaryHostileDestroyed
        {
            get
            {
                var primary = PrimaryHostileContactId;
                if (primary is not { } p || _killedTargets == null) return false;
                var id = Roe.OrderActionMapper.TargetIdToUlong(p);
                return _killedTargets.IsKilled(id);
            }
        }

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
        int maxTechnologyLevel,
        int startEntityKey = 2)
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
        var entityKey = startEntityKey;
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

    private static DatalinkCommsShareState MapDatalinkCommsShareState(CommsState state) =>
        state switch
        {
            CommsState.Degraded => DatalinkCommsShareState.Degraded,
            CommsState.Denied => DatalinkCommsShareState.Denied,
            _ => DatalinkCommsShareState.Nominal,
        };

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
