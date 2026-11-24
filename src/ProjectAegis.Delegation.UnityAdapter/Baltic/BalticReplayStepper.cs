namespace ProjectAegis.Delegation.UnityAdapter.Baltic;

using Bridge;
using Comms;
using Core;
using Data.Catalog;
using Decision;
using Mission;
using Orchestration;
using Policy;
using Projection;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Replay;
using Roe;
using Sim;
using Traits;

/// <summary>
/// H6-02 / DRG-349: shared scenario setup and per-observed-tick stepper extracted from
/// <see cref="BalticReplayHarness"/>. Used identically by <see cref="BalticReplayHarness"/>
/// and the H6 save/resume driver so v2/v3 replay and golden hashes remain byte-identical.
/// </summary>
public sealed class BalticReplayStepper
{
    private readonly ScenarioPolicyProfile? _profile;
    private readonly PdDetectionContactSimulator? _pdSim;
    private readonly ScenarioContactSimulator? _scheduleSim;
    private readonly DatalinkSidePictureMerger? _datalinkMerger;
    private readonly MissionRuntime? _missionRuntime;
    private readonly MissionContactTriggerRuntime? _contactTriggerRuntime;
    private readonly int _sideMaxSalvo;
    private readonly int _checkpointInterval;
    private readonly BalticHeadlessSnapshot _headlessSnapshot;
    private readonly bool _reportWatchTransitions;

    private BalticReplayStepper(
        int seed,
        string scenarioPolicyId,
        ScenarioPolicyProfile? profile,
        PdDetectionContactSimulator? pdSim,
        ScenarioContactSimulator? scheduleSim,
        DatalinkSidePictureMerger? datalinkMerger,
        MissionRuntime? missionRuntime,
        MissionContactTriggerRuntime? contactTriggerRuntime,
        int sideMaxSalvo,
        ReplayCheckpointStore checkpointStore,
        int checkpointInterval,
        DelegationBridge bridge,
        BalticHeadlessSnapshot headlessSnapshot,
        bool reportWatchTransitions)
    {
        Seed = seed;
        ScenarioPolicyId = scenarioPolicyId;
        _profile = profile;
        _pdSim = pdSim;
        _scheduleSim = scheduleSim;
        _datalinkMerger = datalinkMerger;
        _missionRuntime = missionRuntime;
        _contactTriggerRuntime = contactTriggerRuntime;
        _sideMaxSalvo = sideMaxSalvo;
        CheckpointStore = checkpointStore;
        _checkpointInterval = checkpointInterval;
        Bridge = bridge;
        _headlessSnapshot = headlessSnapshot;
        _reportWatchTransitions = reportWatchTransitions;
    }

    public int Seed { get; }

    public string ScenarioPolicyId { get; }

    public DelegationBridge Bridge { get; }

    public SimulationSession? Session => Bridge.Session;

    public ISimWorldSnapshot Snapshot => _headlessSnapshot;

    public IOrderSink Sink => _headlessSnapshot;

    public ReplayCheckpointStore CheckpointStore { get; }

    public ScenarioPolicyProfile? Profile => _profile;

    public PdDetectionContactSimulator? PdSimulator => _pdSim;

    public ulong ObservedTick => (ulong)Math.Max(0, (long)_headlessSnapshot.SimTime);

    public ulong PipelineTick => Bridge.Session?.Sim.Clock.SimTick ?? 0UL;

    public ulong LastSimWorldHash => Bridge.Session?.Sim.LastWorldHash ?? 0UL;

    public ulong DetectionWorldHash => _pdSim?.LastDetectionHash ?? 0UL;

    public ulong CombinedWorldHash => SimWorldHash.Combine(LastSimWorldHash, DetectionWorldHash, 0);

    public static BalticReplayStepper Create(
        int seed,
        string scenarioPolicyId,
        bool mvpEngagement = true,
        ICatalogReader? catalog = null,
        IReadOnlyDictionary<string, bool>? unitReadiness = null,
        IReadOnlyList<ScenarioNearFutureUnitRequest>? nearFutureUnits = null,
        int maxTechnologyLevel = 2,
        IOrderSink? externalOrderSink = null,
        bool reportWatchTransitions = false)
    {
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
                catalogReader,
                DetectionEnvironmentFactory.Build(profile, catalogReader));
            if (profile.DatalinkDoctrine.IsSharingEnabled)
            {
                var datalinkDoctrine = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalogReader);
                datalinkMerger = new DatalinkSidePictureMerger(datalinkDoctrine, detectionTrials);
            }
        }

        if (profile?.ContactSeeds.Count > 0)
        {
            scheduleSim = new ScenarioContactSimulator(profile.ContactSeeds, profile.UnitRadarEmcon);
        }

        var missionRuntime = profile != null ? MissionRuntimeFactory.TryCreate(profile.MissionTimeline) : null;
        var contactTriggerRuntime = profile != null
            ? MissionContactTriggerRuntimeFactory.TryCreate(profile.MissionTimeline, catalogReader)
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

        var gauntletOrbat = RegisterGauntletCatalogUnits(
            bridge,
            scenarioPolicyId,
            catalogReader,
            nextEntityKey,
            bridge.Session);
        nextEntityKey = gauntletOrbat.NextEntityKey;

        // Prefer catalog red unit as hostile target (eliminates synthetic hostile-1 when present).
        var redCombat = gauntletOrbat.Units.FirstOrDefault(u =>
            !u.IsBlue && string.Equals(u.Domain, "surface", StringComparison.OrdinalIgnoreCase))
            ?? gauntletOrbat.Units.FirstOrDefault(u => !u.IsBlue);
        if (redCombat != null)
        {
            hostileBinding = redCombat.Binding;
        }
        // Legacy fallback: joint ORBAT with blue-only units still needs a registry hostile for engage.
        else if (gauntletOrbat.Units.Count > 0 && hostileBinding == null)
        {
            hostileBinding = bridge.Registry.RegisterUnit(new EntityKey(nextEntityKey++), "hostile-1");
        }

        RegisterNearFutureUnits(bridge, nearFutureUnits, maxTechnologyLevel, nextEntityKey);
        var unit = unitBinding.Target;
        // Prefer catalog blue surface as primary unit binding (legacy single-agent path still works).
        var blueCombat = gauntletOrbat.Units.FirstOrDefault(u =>
            u.IsBlue && string.Equals(u.Domain, "surface", StringComparison.OrdinalIgnoreCase))
            ?? gauntletOrbat.Units.FirstOrDefault(u => u.IsBlue);
        if (blueCombat != null)
        {
            unit = blueCombat.Binding.Target;
        }

        var patrolPolicyFactory = profile?.DelegationSettings.UsePatrolCandidates == true
            ? (Func<EngagePrimaryMode, IPolicy>)(mode => new PatrolCandidateEngagePolicy(mode))
            : _ => new EngageOnlyPolicy();

        // Multi-domain engage: assign an engage agent to EVERY blue catalog unit (surface/air/sub)
        // so air and subsurface can True|Launched — not detection-only CATALOG_UNIT registration.
        var blueUnits = gauntletOrbat.Units.Where(u => u.IsBlue).ToList();
        if (blueUnits.Count > 0)
        {
            var agentIndex = 0;
            foreach (var blue in blueUnits)
            {
                var blueUnitKey = blue.PlatformId;
                var bluePolicy = profile?.ResolveForUnit(blueUnitKey, isFriendly: true)
                    ?? profile?.ResolveForUnit("u1", isFriendly: true)
                    ?? EffectivePolicy.DefaultFree;
                var agentId = agentIndex == 0 ? "a1" : $"a-blue-{agentIndex}";
                agentIndex++;
                var agent = bridge.Orchestrator.CreateAgent(
                    new AgentId(agentId),
                    PersonalityCatalog.All[0].Traits,
                    AutonomyLevel.FullAutonomous,
                    policy: patrolPolicyFactory(EngagePrimaryMode.Hostile));
                bridge.Orchestrator.AssignAgentToTarget(
                    agent,
                    blue.Binding.Target,
                    bluePolicy,
                    isFriendly: true);
            }
        }
        else
        {
            // Legacy synthetic path (ReplayGolden / non-gauntlet).
            var blueUnitKey = "u1";
            var bluePolicy = profile?.ResolveForUnit(blueUnitKey, isFriendly: true)
                ?? EffectivePolicy.DefaultFree;
            var agent = bridge.Orchestrator.CreateAgent(
                new AgentId("a1"),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.FullAutonomous,
                policy: patrolPolicyFactory(EngagePrimaryMode.Hostile));
            bridge.Orchestrator.AssignAgentToTarget(agent, unit, bluePolicy, isFriendly: true);
        }

        if (hostileBinding != null)
        {
            var redUnitKey = redCombat?.PlatformId ?? "hostile-1";
            var redPolicy = profile?.ResolveForUnit(redUnitKey, isFriendly: false)
                ?? profile?.ResolveForUnit("hostile-1", isFriendly: false)
                ?? EffectivePolicy.DefaultFree;
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

        // Detection observer->target pairs enable concurrent multi-domain shooters
        // (SwarmSalvoDeconfliction allows one shooter per distinct target).
        IReadOnlyDictionary<string, string>? preferredHostileByShooter = null;
        if (profile?.DetectionTrials is { Count: > 0 })
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var trial in profile.DetectionTrials)
            {
                if (string.IsNullOrWhiteSpace(trial.ObserverId)
                    || string.IsNullOrWhiteSpace(trial.TargetId))
                {
                    continue;
                }

                // Prefer first trial per observer (stable order from profile).
                if (!map.ContainsKey(trial.ObserverId))
                {
                    map[trial.ObserverId] = trial.TargetId;
                }
            }

            if (map.Count > 0)
            {
                preferredHostileByShooter = map;
            }
        }

        var snapshot = new BalticHeadlessSnapshot(
            pdSim,
            scheduleSim,
            profile?.UnitRadarEmcon,
            bridge.Session?.KilledTargets,
            fallbackContactCount: 2,
            fallbackHasTrack: true,
            preferredHostileByShooter,
            externalOrderSink);

        return new BalticReplayStepper(
            seed,
            scenarioPolicyId,
            profile,
            pdSim,
            scheduleSim,
            datalinkMerger,
            missionRuntime,
            contactTriggerRuntime,
            sideMaxSalvo,
            checkpointStore,
            checkpointInterval,
            bridge,
            snapshot,
            reportWatchTransitions);
    }

    public DelegationTickResult StepOneObservedTick()
    {
        _headlessSnapshot.Advance(1.0);
        var simTick = (ulong)Math.Max(0, (long)_headlessSnapshot.SimTime);

        if (_missionRuntime != null)
        {
            foreach (var emission in _missionRuntime.Tick(simTick, _headlessSnapshot.SimTime, 0))
            {
                switch (emission.Event.Kind)
                {
                    case MissionEventKind.MissionTransition:
                        Bridge.Orchestrator.OrderLog.Append(
                            OrderLogEntryFactories.FromMissionTransition(
                                new MissionTransitionRecord(
                                    emission.SequenceId,
                                    emission.SimTime,
                                    emission.SimTick,
                                    emission.Event.EventId,
                                    emission.Event.Code)));
                        break;
                    case MissionEventKind.EventFired:
                        Bridge.Orchestrator.OrderLog.Append(
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

        if (_pdSim != null && _profile != null)
        {
            _pdSim.SetCommsStaleThresholdDivisor(
                CommsTrackStaleness.StaleThresholdDivisor(Bridge.CurrentCommsState, _profile.CommsDisplay));
        }

        IReadOnlyList<ContactTransition> transitions = _pdSim != null
            ? _pdSim.Tick(simTick, _headlessSnapshot.SimTime)
            : Array.Empty<ContactTransition>();
        if (_datalinkMerger != null)
        {
            var shared = _datalinkMerger.Merge(
                transitions,
                simTick,
                _headlessSnapshot.SimTime,
                MapDatalinkCommsShareState(Bridge.CurrentCommsState));
            if (shared.Count > 0)
            {
                // Order preserved (transitions then shared) for identical order-log appends and hash.
                var merged = new List<ContactTransition>(transitions.Count + shared.Count);
                merged.AddRange(transitions);
                merged.AddRange(shared);
                transitions = merged.ToArray();
            }
        }

        if (_scheduleSim != null)
        {
            var scheduled = _scheduleSim.Tick(simTick, _headlessSnapshot.SimTime);
            if (scheduled.Count > 0)
            {
                if (transitions.Count == 0)
                {
                    transitions = scheduled;
                }
                else
                {
                    var merged = new List<ContactTransition>(transitions.Count + scheduled.Count);
                    merged.AddRange(transitions);
                    merged.AddRange(scheduled);
                    transitions = merged.ToArray();
                }
            }
        }

        foreach (var transition in transitions)
        {
            if (_contactTriggerRuntime != null)
            {
                foreach (var emission in _contactTriggerRuntime.Evaluate(transition, _headlessSnapshot.SimTime, simTick))
                {
                    var trigger = emission.Trigger;
                    Bridge.Orchestrator.OrderLog.Append(
                        OrderLogEntryFactories.FromMissionTransition(
                            new MissionTransitionRecord(
                                0,
                                emission.SimTime,
                                emission.SimTick,
                                trigger.TriggerId,
                                trigger.MissionCode)));
                    var isFriendly = trigger.PolicySide == MissionContactPolicySide.Friendly;
                    var roePolicy = new EffectivePolicy(trigger.Roe, _sideMaxSalvo);
                    Bridge.Orchestrator.ApplyRoeToUnits(
                        trigger.UnitIds,
                        roePolicy,
                        isFriendly,
                        emission.SimTime,
                        emission.SimTick);
                }
            }

            Bridge.Orchestrator.OrderLog.AppendContactTransition(transition);
        }

        if (_reportWatchTransitions && transitions.Count > 0)
        {
            Bridge.Session?.ReportContactTransitions(transitions);
        }

        var tickResult = Bridge.Tick(_headlessSnapshot, _headlessSnapshot);

        if (Bridge.Session?.KilledTargets is { } killed && _pdSim != null)
        {
            foreach (var (_, label) in killed.DrainNewKills())
            {
                foreach (var killTransition in _pdSim.ApplyTargetKill(simTick, _headlessSnapshot.SimTime, label))
                {
                    Bridge.Orchestrator.OrderLog.AppendContactTransition(killTransition);
                }
            }
        }

        if (Bridge.Session?.BdaContactLifecycleRegistry is { } bdaLifecycle && _pdSim != null)
        {
            foreach (var lostTransition in BdaContactLifecycleHotTickApplier.ApplyFromRegistry(
                         _pdSim,
                         simTick,
                         _headlessSnapshot.SimTime,
                         bdaLifecycle))
            {
                Bridge.Orchestrator.OrderLog.AppendContactTransition(lostTransition);
            }
        }

        if (_checkpointInterval > 0 && simTick % (ulong)_checkpointInterval == 0)
        {
            var simHashTick = Bridge.Session?.Sim.LastWorldHash ?? 0;
            var detectionHashTick = _pdSim?.LastDetectionHash ?? 0;
            var worldHashTick = SimWorldHash.Combine(simHashTick, detectionHashTick, 0);
            CheckpointStore.Record(
                simTick,
                worldHashTick,
                Bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
                Bridge.Orchestrator.DecisionLog.ChronologicalEntries().LastOrDefault()?.SequenceId ?? 0);
        }

        return tickResult;
    }

    public BalticReplayHarness.Result BuildResult(int ticks)
    {
        var simHash = Bridge.Session?.Sim.LastWorldHash ?? 0;
        var detectionHash = _pdSim?.LastDetectionHash ?? 0;
        var worldHash = SimWorldHash.Combine(simHash, detectionHash, 0);

        var messages = MessageLogProjection.Project(Bridge.Orchestrator.DecisionLog);
        var sensorC2 = SensorC2Bridge.Build(_headlessSnapshot, Bridge.Orchestrator.DecisionLog);
        var scoringCsv = LossesScoringCsvExporter.FormatRow(
            ScenarioPolicyId,
            Seed,
            "BLUE",
            Bridge.Orchestrator.DecisionLog);
        var fingerprint = Bridge.Orchestrator.DecisionLog.ComputeFingerprint();
        var fireOrder = BalticReplayHarness.ResolveFireOrder(_profile?.MissionTimeline, Bridge.Orchestrator.DecisionLog);
        return new BalticReplayHarness.Result(
            Seed,
            ScenarioPolicyId,
            ticks,
            fingerprint,
            OrderLogReplayFingerprint.ComputeSha256Hex(Bridge.Orchestrator.DecisionLog),
            Bridge.Orchestrator.DecisionLog.Engagements.Count,
            detectionHash,
            worldHash,
            CheckpointStore.Checkpoints,
            messages,
            sensorC2,
            scoringCsv,
            Bridge.Orchestrator.DecisionLog,
            fireOrder);
    }

    public sealed class BalticHeadlessSnapshot : ISimWorldSnapshot, IOrderSink
    {
        private readonly PdDetectionContactSimulator? _pd;
        private readonly ScenarioContactSimulator? _schedule;
        private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
        private readonly KilledTargetRegistry? _killedTargets;
        private readonly int _fallbackContactCount;
        private readonly bool _fallbackHasTrack;
        private readonly IReadOnlyDictionary<string, string>? _preferredHostileByShooter;
        private readonly IOrderSink? _externalOrderSink;
        private double _simTime;

        public BalticHeadlessSnapshot(
            PdDetectionContactSimulator? pd,
            ScenarioContactSimulator? schedule,
            IReadOnlyDictionary<string, EmconState>? unitRadarEmcon,
            KilledTargetRegistry? killedTargets,
            int fallbackContactCount,
            bool fallbackHasTrack,
            IReadOnlyDictionary<string, string>? preferredHostileByShooter = null,
            IOrderSink? externalOrderSink = null)
        {
            _pd = pd;
            _schedule = schedule;
            _unitRadarEmcon = unitRadarEmcon;
            _killedTargets = killedTargets;
            _fallbackContactCount = fallbackContactCount;
            _fallbackHasTrack = fallbackHasTrack;
            _preferredHostileByShooter = preferredHostileByShooter;
            _externalOrderSink = externalOrderSink;
        }

        public IReadOnlyDictionary<string, string>? PreferredHostileByShooter =>
            _preferredHostileByShooter;

        public double SimTime => _simTime;

        public int ContactCount =>
            _pd?.ActiveCount ?? _schedule?.ActiveCount ?? _fallbackContactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryBlueForceContactId
        {
            get
            {
                string? candidate = _pd?.PrimaryBlueForceTargetId;
                if (candidate != null && BalticV3SideRegistry.IsBlueForceUnit(candidate))
                {
                    return new TargetId(candidate);
                }

                // Catalog ORBAT: red can engage blue even without reverse detection trials.
                var fallback = BalticV3SideRegistry.GetDefaultBlueUnitId();
                if (fallback != null && BalticV3SideRegistry.IsBlueForceUnit(fallback))
                {
                    return new TargetId(fallback);
                }

                return null;
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

                var id = OrderActionMapper.TargetIdToUlong(p);
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
                var id = OrderActionMapper.TargetIdToUlong(p);
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

            var id = OrderActionMapper.TargetIdToUlong(memberId);
            return !_killedTargets.IsKilled(id);
        }

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _externalOrderSink?.ApplyOrder(entity, in order);
    }

    private sealed record GauntletRegisteredUnit(
        string PlatformId,
        string Domain,
        bool IsBlue,
        SimEntityBinding Binding);

    private sealed record GauntletOrbatRegistration(
        int NextEntityKey,
        IReadOnlyList<GauntletRegisteredUnit> Units);

    private static GauntletOrbatRegistration RegisterGauntletCatalogUnits(
        DelegationBridge bridge,
        string scenarioPolicyId,
        ICatalogReader catalogReader,
        int startEntityKey,
        SimulationSession? session)
    {
        var dto = Data.Scenario.ScenarioPolicyJsonCatalog.TryGetJson(scenarioPolicyId);
        var units = dto?.Gauntlet?.Units;
        if (units == null || units.Count == 0)
        {
            return new GauntletOrbatRegistration(startEntityKey, Array.Empty<GauntletRegisteredUnit>());
        }

        var entityKey = startEntityKey;
        var registered = new List<GauntletRegisteredUnit>();
        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.PlatformId))
            {
                continue;
            }

            var unitId = unit.PlatformId;
            var domain = string.IsNullOrWhiteSpace(unit.Domain) ? "surface" : unit.Domain;
            var isBlue = !string.Equals(unit.Side, "red", StringComparison.OrdinalIgnoreCase);
            BalticV3SideRegistry.RegisterScenarioSide(unitId, isBlue ? "blue" : "red");
            var key = entityKey++;
            var binding = bridge.Registry.RegisterUnit(new EntityKey(key), unitId);

            var maxRange = 0.0;
            foreach (var mag in catalogReader.GetSortedMagazines())
            {
                if (!string.Equals(mag.PlatformId, unit.PlatformId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (catalogReader.TryGetWeaponEnvelope(mag.WeaponId, out var env)
                    && env.MaxRangeMeters > maxRange)
                {
                    maxRange = env.MaxRangeMeters;
                }
            }

            var seededRounds = 0;
            if (session?.Magazines != null && maxRange > 0)
            {
                CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
                    session.Magazines,
                    catalogReader,
                    unit.PlatformId,
                    (ulong)key,
                    mountId: 0,
                    fallbackRounds: Math.Max(1, session.DefaultMagazineRounds ?? 4),
                    out seededRounds);
            }

            bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(
                0,
                0,
                0,
                $"CATALOG_UNIT:{unit.PlatformId}:{domain}",
                unitId));
            bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(
                0,
                0,
                0,
                $"MAGAZINE_SEED:{unit.PlatformId}:{seededRounds}:{maxRange.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                unitId));

            registered.Add(new GauntletRegisteredUnit(
                unit.PlatformId,
                domain,
                isBlue,
                binding));
        }

        return new GauntletOrbatRegistration(entityKey, registered);
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
