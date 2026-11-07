namespace ProjectAegis.Delegation.UnityAdapter.Gameplay;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Gameplay;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Headless application service for Baltic Passage. Owns the authoritative world and command
/// validation; Unity consumes the snapshot and presentation and forwards commands only.
/// Combat and ordnance use the existing delegation and MVP engagement resolver.
/// </summary>
public sealed class EscortMissionSession : ISimWorldSnapshot, IOrderSink
{
    /// <summary>Friendly escort identifier used by existing C2 unit panels.</summary>
    public const string EscortId = "u1";
    /// <summary>The protected civilian convoy.</summary>
    public const string ConvoyId = "blue-convoy";
    /// <summary>Potentially hostile surface unit.</summary>
    public const string ThreatId = "hostile-1";
    /// <summary>Player-visible contact reference.</summary>
    public const string ThreatContactId = "escort-contact-1";
    private readonly int _seed;
    private readonly EscortMissionDefinition _definition;
    private readonly List<EscortDecisiveMoment> _moments = new();
    private readonly List<EscortDecisiveMoment> _commandHistory = new();
    private EscortMissionState _world;
    private AgentController? _friendlyAgent;
    private int _observedOutcomeCount;
    private string _lastContactState = "Unknown";
    private int _unauthorizedReleases;

    /// <summary>Constructs a fresh, paused planning session without smoke-data seeding.</summary>
    public EscortMissionSession(int seed = 42, EscortMissionDefinition? definition = null)
    {
        _seed = seed;
        _definition = definition ?? EscortMissionDefinition.LoadDefault();
        _definition.Validate();
        _world = new EscortMissionState(_definition);
        Bridge = CreateBridge();
    }

    /// <summary>Delegation composition root exposed for existing headless C2 command/projection adapters.</summary>
    public DelegationBridge Bridge { get; private set; }
    /// <summary>Read-only world snapshot for map, OOB and sensor projections.</summary>
    public ISimWorldSnapshot Snapshot => this;
    /// <summary>Mission fixed step in seconds.</summary>
    public double FixedDeltaSeconds => EscortMissionState.FixedDeltaSeconds;
    /// <summary>Elapsed authoritative simulation time.</summary>
    public double SimTime => _world.SimTime;
    /// <summary>Terminal mission state.</summary>
    public bool IsComplete => _world.IsComplete;
    /// <summary>Whether gameplay is paused or still in planning.</summary>
    public bool IsPaused => Bridge.Phase == SimulationPhase.Planning || Bridge.Session!.IsSimPaused;
    /// <summary>Exit marker position in the same normalized coordinates as mission map symbols.</summary>
    public double ExitNormalizedX => 0.1 + _definition.ExitDistanceNm / _definition.TheaterWidthNm * 0.8;
    /// <summary>Player-selected time acceleration; applied only by the external frame runner.</summary>
    public int AccelerationFactor => Bridge.Session!.TimeAccelerationFactor;
    /// <summary>Number of contacts actually detected by the player's sensors.</summary>
    public int ContactCount => _world.ContactState == "Unknown" || !_world.ThreatAlive ? 0 : 1;
    /// <summary>Engagements resolve atomically in this mission slice.</summary>
    public int ActiveEngagementCount => 0;
    /// <summary>Identified friendly fire-control solution; never exposes enemy sensor state.</summary>
    public bool HasFireControlTrackOnPrimaryContact => _world.HasFriendlyTrack;
    /// <summary>Player-selected radar emissions posture.</summary>
    public bool ObserverRadarEmconActive => _world.RadarActive;
    /// <summary>Primary hostile contact only after player detection.</summary>
    public TargetId? PrimaryHostileContactId => ContactCount > 0 ? new TargetId(ThreatId) : null;
    /// <summary>Confirmed hostile destruction.</summary>
    public bool PrimaryHostileDestroyed => !_world.ThreatAlive;
    /// <summary>Red's target is the convoy; this is consumed by combat, not shown as player knowledge.</summary>
    public TargetId? PrimaryBlueForceContactId => new TargetId(ConvoyId);
    /// <summary>Confirmed convoy destruction.</summary>
    public bool PrimaryBlueForceContactDestroyed => !_world.ConvoyAlive;

    /// <summary>Player-facing mission view, built on request rather than once per simulation frame.</summary>
    public EscortMissionPresentation Presentation => BuildPresentation();

    /// <summary>Known friendly units and blue-observed contacts only; never enumerates hidden red registry units.</summary>
    public IReadOnlyList<MapSymbolEntry> MapSymbols
    {
        get
        {
            var symbols = new List<MapSymbolEntry>(3);
            AddMapSymbol(symbols, EscortId, "Friendly", "■", "Escort", !_world.EscortAlive);
            AddMapSymbol(symbols, ConvoyId, "Friendly", "■", "Civilian convoy", !_world.ConvoyAlive);
            if (_world.ContactState != "Unknown")
                AddMapSymbol(symbols, ThreatContactId, _world.ContactState == "Identified" || !_world.ThreatAlive ? "Hostile" : "Unknown", "◆",
                    _world.ThreatAlive ? $"Surface contact — {_world.ContactState}" : "Surface contact — confirmed destroyed", !_world.ThreatAlive);
            return symbols.ToArray();
        }
    }

    private void AddMapSymbol(List<MapSymbolEntry> symbols, string id, string affiliation, string glyph, string label, bool destroyed)
    {
        if (!TryGetKinematicPose(id, out var pose)) return;
        symbols.Add(new MapSymbolEntry(id, affiliation, glyph, label, pose.NormalizedX!.Value, pose.NormalizedY!.Value,
            destroyed, HasAuthoritativePose: true, CourseDeg: pose.CourseDeg, SpeedNmPerHour: pose.SpeedNmPerHour));
    }

    /// <summary>Applies one fixed step; paused, planning and completed sessions do not consume time.</summary>
    public void Step(double fixedDeltaSeconds)
    {
        if (double.IsNaN(fixedDeltaSeconds) || double.IsInfinity(fixedDeltaSeconds)
            || Math.Abs(fixedDeltaSeconds - FixedDeltaSeconds) > 1e-10)
            throw new ArgumentOutOfRangeException(nameof(fixedDeltaSeconds), "Use the mission's fixed simulation step.");
        if (IsPaused || IsComplete) return;
        _world.Advance();
        var contactAlert = ObserveContact();
        Bridge.Tick(this, this);
        _world.AdvanceKinematics();
        ObserveCombat();
        if (contactAlert is not null) Bridge.Session!.ReportWatchAttention(contactAlert);
        _world.EvaluateObjective();
        if (IsComplete)
        {
            AddMoment("Mission complete", _world.Outcome);
            Bridge.Session!.PauseSim();
            Bridge.FinalizeScenario(_world.Succeeded, _world.Succeeded ? 1 : 0);
        }
    }

    /// <summary>Validates a mission command, then delegates unit orders through existing human queues.</summary>
    public EscortCommandResult Command(string commandId)
    {
        if (commandId == "restart") { Restart(); return Accepted("Mission reset with the same seed."); }
        if (IsComplete) return Rejected("Mission is complete. Restart to try another plan.");
        switch (commandId)
        {
            case "begin":
                if (Bridge.Phase != SimulationPhase.Planning) return Rejected("Execution has already begun.");
                Bridge.BeginExecution();
                Bridge.Session!.ResumeSim();
                RecordCommand("Begin execution", "Convoy transit commenced under the current doctrine.");
                return Accepted("Execution begun.");
            case "pause": Bridge.Session!.PauseSim(); return Accepted("Simulation paused.");
            case "resume":
                if (Bridge.Phase == SimulationPhase.Planning) return Rejected("Read the briefing, then begin execution.");
                return Bridge.Session!.TryResumeSim() ? Accepted("Simulation resumed.") : Rejected("Acknowledge pending alerts or resolve pending approvals before resuming.");
            case "acknowledge-alerts":
                foreach (var card in Bridge.Session!.WatchQueue.SnapshotVisible())
                    Bridge.Session.WatchQueue.TryAcknowledge(card.EventId);
                return Accepted("Visible alerts acknowledged. Resume when ready.");
            case "radar-active": case "investigate":
                _world.SetRadar(true);
                RecordCommand("Radar active", "Active observation improves identification and exposes emissions to the opponent.");
                return Accepted("Radar active: faster identification, greater exposure.");
            case "radar-passive":
                _world.SetRadar(false);
                RecordCommand("Radar passive", "Passive observation reduces emissions and takes longer to identify the contact.");
                return Accepted("Radar passive: slower identification, lower exposure.");
            case "posture-cautious": return AssignPosture(EscortPosture.Cautious);
            case "posture-balanced": return AssignPosture(EscortPosture.Balanced);
            case "posture-assertive": return AssignPosture(EscortPosture.Assertive);
            case "posture-manual": return AssignPosture(EscortPosture.Manual);
            case "hold": return QueueConvoyOrder(OrderKind.Hold);
            case "proceed": return QueueConvoyOrder(OrderKind.Move);
            case "engage": return QueueEngage();
            case "alerts-all": Bridge.Session!.WatchPauseGate.InterruptionMode = WatchInterruptionMode.All; return Accepted("Pause for all new tactical alerts.");
            case "alerts-critical": Bridge.Session!.WatchPauseGate.InterruptionMode = WatchInterruptionMode.CriticalOnly; return Accepted("Pause for critical tactical alerts.");
            case "alerts-manual": Bridge.Session!.WatchPauseGate.InterruptionMode = WatchInterruptionMode.Manual; return Accepted("Tactical auto-pause disabled; approval requirements remain enforced.");
            case "speed-faster":
                Bridge.Session!.SetTimeAccelerationFactor(Math.Min(256, AccelerationFactor * 2));
                return Accepted($"Time acceleration {AccelerationFactor}x.");
            case "speed-slower":
                Bridge.Session!.SetTimeAccelerationFactor(Math.Max(1, AccelerationFactor / 2));
                return Accepted($"Time acceleration {AccelerationFactor}x.");
            default: return Rejected("Unknown mission command.");
        }
    }

    /// <summary>Recreates every mission-owned object with the original seed and definition.</summary>
    public void Restart()
    {
        _world = new EscortMissionState(_definition);
        _moments.Clear();
        _commandHistory.Clear();
        _observedOutcomeCount = 0;
        _lastContactState = "Unknown";
        _unauthorizedReleases = 0;
        _friendlyAgent = null;
        Bridge = CreateBridge();
    }

    /// <summary>Authoritative registered-unit liveness.</summary>
    public bool IsMemberAlive(TargetId memberId) => memberId.Value switch
    {
        EscortId => _world.EscortAlive,
        ConvoyId => _world.ConvoyAlive,
        ThreatId => _world.ThreatAlive,
        _ => false,
    };

    /// <summary>Player-known poses, with hidden hostile positions withheld until detection.</summary>
    public bool TryGetKinematicPose(string unitOrContactId, out UnitKinematicPose pose)
    {
        pose = default;
        double x;
        double y = 0;
        double speed;
        switch (unitOrContactId)
        {
            case EscortId: x = _world.EscortXNm; speed = _world.EscortHolding || !_world.EscortAlive ? 0 : _definition.EscortSpeedKnots; break;
            case ConvoyId: x = _world.ConvoyXNm; speed = _world.ConvoyHolding || !_world.ConvoyAlive ? 0 : _definition.ConvoySpeedKnots; break;
            case ThreatId: case ThreatContactId:
                if (_world.ContactState == "Unknown") return false;
                x = _definition.EnemyStartXNm; y = _definition.EnemyStartYNm; speed = 0; break;
            default: return false;
        }
        pose = new UnitKinematicPose(null, null,
            (float)(0.1 + x / _definition.TheaterWidthNm * 0.8),
            (float)(0.5 + y / _definition.TheaterWidthNm * 0.8), 90, (float)speed);
        return true;
    }

    /// <summary>Applies unit orders only after delegation/human queue dispatch, never directly from the UI.</summary>
    public void ApplyOrder(EntityKey entity, in Order order)
    {
        if (order.Target.Value == ConvoyId)
        {
            if (order.Kind == OrderKind.Hold) _world.SetConvoyHolding(true);
            else if (order.Kind == OrderKind.Move) _world.SetConvoyHolding(false);
        }
        else if (order.Target.Value == EscortId && _world.Posture == EscortPosture.Manual)
        {
            if (order.Kind == OrderKind.Hold) _world.SetEscortHolding(true);
            else if (order.Kind is OrderKind.Move or OrderKind.ReturnToBase) _world.SetEscortHolding(false);
        }
        if (order.Target.Value == EscortId && order.Kind is OrderKind.SetEmcon or OrderKind.SetSensors)
            _world.SetRadar(!_world.RadarActive);
    }

    private DelegationBridge CreateBridge()
    {
        var bridge = new DelegationBridge(_seed);
        bridge.Orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree)
        {
            Id = _definition.Id,
        };
        var convoy = bridge.Registry.RegisterUnit(new EntityKey(2), ConvoyId);
        var escort = bridge.Registry.RegisterUnit(new EntityKey(1), EscortId);
        var threat = bridge.Registry.RegisterUnit(new EntityKey(3), ThreatId);
        convoy.Target.Slot.SetActive(new HumanController());
        escort.Target.Slot.SetActive(new HumanController());
        // Legacy IDs u1 / hostile-1 already have stable side identities. Avoid global scenario-side state.
        var traits = new TraitVector(0.5, 0.5, _definition.EnemyReactionSeconds, 0, 1, 1);
        var enemy = bridge.Orchestrator.CreateAgent(new AgentId("escort-opponent"), traits,
            AutonomyLevel.FullAutonomous, policy: new EscortPolicy(() => new EscortPolicyFacts(
                _world.EnemyHasTrack && _world.RangeNm(true) <= _definition.EnemyWeaponRangeNm,
                _world.ConvoyAlive, GetRounds(ThreatId), 0)));
        bridge.Orchestrator.AssignAgentToTarget(enemy, threat.Target, EffectivePolicy.DefaultFree, isFriendly: false);
        bridge.EnableMvpEngagement(new EngageContext(1, new WeaponEnvelope(0, _definition.FriendlyWeaponRangeNm * 1852),
            _definition.FriendlyRounds, false, PkBase: _definition.FriendlyPk), _definition.FriendlyRounds);
        bridge.Session!.ExternallyScheduledSteps = true;
        bridge.Session.ResolveEngagementContext = ResolveEngagementContext;
        bridge.Session.Magazines!.SetRounds(OrderActionMapper.TargetIdToUlong(new TargetId(EscortId)), 0, _definition.FriendlyRounds);
        bridge.Session.Magazines.SetRounds(OrderActionMapper.TargetIdToUlong(new TargetId(ThreatId)), 0, _definition.EnemyRounds);
        bridge.Session.PauseSim();
        return bridge;
    }

    private EngageContext ResolveEngagementContext(string shooterId, EngageContext primed)
    {
        var enemy = shooterId == ThreatId;
        return primed with
        {
            RangeMeters = _world.RangeNm(enemy) * 1852,
            Envelope = new WeaponEnvelope(0, (enemy ? _definition.EnemyWeaponRangeNm : _definition.FriendlyWeaponRangeNm) * 1852),
            HasFireControlTrack = enemy ? _world.EnemyHasTrack : _world.HasFriendlyTrack,
            RadarEmconActive = enemy || _world.HasFriendlyTrack,
            ContactIdentified = enemy || _world.ContactState == "Identified",
            PkBase = enemy ? _definition.EnemyPk : _definition.FriendlyPk,
        };
    }

    private EscortCommandResult AssignPosture(EscortPosture posture)
    {
        _world.SetPosture(posture);
        var target = Bridge.Registry.Bindings.First(b => b.TargetId.Value == EscortId).Target;
        if (posture == EscortPosture.Manual)
        {
            // Re-selecting manual control must retain orders already queued by the player.
            if (target.Slot.Active is not HumanController) target.Slot.SetActive(new HumanController());
        }
        else
        {
            var traits = new TraitVector(0.5, 0.5, _definition.AgentReactionSeconds, 0, 1, 1);
            _friendlyAgent ??= Bridge.Orchestrator.CreateAgent(new AgentId("escort-officer"), traits,
                AutonomyLevel.FullAutonomous, policy: new EscortPolicy(() => new EscortPolicyFacts(
                    _world.HasFriendlyTrack && _world.RangeNm(false) <= _definition.FriendlyWeaponRangeNm,
                    _world.ThreatAlive, GetRounds(EscortId), ReserveRounds())));
            Bridge.Orchestrator.AssignAgentToTarget(_friendlyAgent, target, EffectivePolicy.DefaultFree);
        }
        RecordCommand("Delegation changed", $"Escort posture: {posture}; defensive reserve {ReserveRounds()} rounds.");
        return Accepted($"Escort {posture}: retains {ReserveRounds()} defensive rounds.");
    }

    private EscortCommandResult QueueConvoyOrder(OrderKind kind)
    {
        if (!Bridge.TryEnqueueHumanOrder(new EntityKey(2), kind, SimTime)) return Rejected("Convoy order could not be queued.");
        RecordCommand(kind == OrderKind.Hold ? "Hold convoy" : "Proceed convoy", "Convoy movement order queued for execution.");
        return Accepted(kind == OrderKind.Hold
            ? "Convoy hold queued. Escort movement, threat activity and the deadline continue."
            : "Convoy transit queued. Keep the escort close and protect the remaining route.");
    }

    private EscortCommandResult QueueEngage()
    {
        if (!_world.ThreatAlive) return Rejected("Threat is already confirmed destroyed.");
        if (!_world.HasFriendlyTrack) return Rejected("Identify the contact and acquire a fire-control track before engaging.");
        if (_world.RangeNm(false) > _definition.FriendlyWeaponRangeNm) return Rejected("Reposition into the weapon's launch envelope.");
        if (GetRounds(EscortId) == 0) return Rejected("Magazine empty. Preserve the convoy and withdraw from the threat.");
        if (_world.Posture != EscortPosture.Manual) return Rejected("Select manual control before issuing a direct weapon order.");
        return Bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Engage, SimTime)
            ? Accepted("Weapon order queued for the next simulation step.") : Rejected("Weapon order could not be queued.");
    }

    private WatchAttentionEvent? ObserveContact()
    {
        if (_world.ContactState != _lastContactState)
        {
            Bridge.Orchestrator.DecisionLog.AppendContactChange(new ContactChangeRecord(0, SimTime,
                (ulong)(SimTime / FixedDeltaSeconds), EscortId, ThreatContactId, ThreatId, _lastContactState, _world.ContactState));
            var first = _lastContactState == "Unknown";
            _lastContactState = _world.ContactState;
            AddMoment("Contact update", $"Surface contact {_world.ContactState.ToLowerInvariant()} using {(_world.RadarActive ? "active" : "passive")} observation.");
            if (first) return new WatchAttentionEvent("escort:contact:first",
                WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, (ulong)(SimTime / FixedDeltaSeconds),
                ThreatContactId, "escort:surface", "New surface contact: investigate before engaging.");
        }
        // Enemy targeting remains hidden. The player sees only their own emissions decision, not the red track state.
        return null;
    }

    private void ObserveCombat()
    {
        var outcomes = Bridge.Orchestrator.DecisionLog.EngagementOutcomes;
        for (var i = _observedOutcomeCount; i < outcomes.Count; i++)
        {
            var outcome = outcomes[i];
            var detail = outcome.ShooterTargetId.Value == EscortId
                ? $"Escort engaged the identified surface contact: {outcome.OutcomeCode}."
                : _world.ContactState == "Identified"
                    ? $"Identified surface threat engaged the convoy: {outcome.OutcomeCode}."
                    : outcome.OutcomeCode == EngagementOutcomeCodes.Kill
                        ? "Convoy destroyed by incoming fire; attacker identity remains unknown."
                        : "Incoming fire reported near the convoy; attacker identity remains unknown.";
            AddMoment("Combat outcome", detail);
            if (outcome.OutcomeCode == EngagementOutcomeCodes.Kill
                && outcome.VictimTargetId.Value is ConvoyId or EscortId)
                Bridge.Session!.ReportWatchAttention(new WatchAttentionEvent(
                    "escort:loss:" + outcome.VictimTargetId.Value, WatchAttentionKind.OwnSideLossOrDamage,
                    WatchAttentionPriority.Critical, (ulong)(SimTime / FixedDeltaSeconds), outcome.VictimTargetId.Value,
                    ReasonDetail: detail));
            if (outcome.ShooterTargetId.Value == EscortId && _world.ContactState != "Identified") _unauthorizedReleases++;
        }
        _observedOutcomeCount = outcomes.Count;
        _world.ObserveCombat(IsAlive(EscortMissionSession.ConvoyId), IsAlive(EscortId), IsAlive(ThreatId));
    }

    private bool IsAlive(string id) => !Bridge.Session!.KilledTargets!.IsKilled(OrderActionMapper.TargetIdToUlong(new TargetId(id)));
    private int GetRounds(string id) => Bridge?.Session?.Magazines?.GetRounds(OrderActionMapper.TargetIdToUlong(new TargetId(id)), 0) ?? 0;
    private int ReserveRounds() => _world.Posture switch
    {
        EscortPosture.Cautious => _definition.CautiousReserveRounds,
        EscortPosture.Balanced => _definition.BalancedReserveRounds,
        _ => 0,
    };

    private void RecordCommand(string title, string detail)
    {
        var moment = new EscortDecisiveMoment(SimTime, title, detail);
        _commandHistory.Add(moment);
        AddMoment(title, detail);
        Bridge.Orchestrator.DecisionLog.AppendEventFired(new EventFiredRecord(0, SimTime,
            (ulong)(SimTime / FixedDeltaSeconds), $"escort:command:{_commandHistory.Count}", title));
    }

    private void AddMoment(string title, string detail) => _moments.Add(new EscortDecisiveMoment(SimTime, title, detail));
    private static EscortCommandResult Accepted(string message) => new(true, message);
    private static EscortCommandResult Rejected(string message) => new(false, message);

    private EscortMissionPresentation BuildPresentation()
    {
        var spent = _definition.FriendlyRounds - GetRounds(EscortId);
        var score = (_world.Succeeded ? _definition.SuccessPoints : 0)
            + (_world.ConvoyAlive ? _definition.ConvoySurvivalPoints : 0)
            + (_world.EscortAlive ? _definition.EscortSurvivalPoints : 0)
            - spent * _definition.RoundExpenditurePenalty - _unauthorizedReleases * _definition.ViolationPenalty;
        var status = IsComplete ? (_world.Succeeded ? "Success — " : "Failure — ") + _world.Outcome
            : Bridge.Phase == SimulationPhase.Planning ? "Planning — set doctrine, then begin execution"
            : IsPaused ? "Paused — review alerts and orders" : "Executing — protect the convoy";
        var guidance = _world.Posture == EscortPosture.Manual && Bridge.Phase == SimulationPhase.Planning
            ? "Assign an escort officer or retain manual control. Review radar exposure and missile reserves before beginning."
            : _world.ContactState == "Unknown" ? "Observe the passage. Active radar acquires contacts quickly but broadcasts your position."
            : !_world.HasFriendlyTrack && _world.ThreatAlive ? "Investigate the surface contact. Build an identified fire-control track before releasing weapons."
            : _world.ThreatAlive && GetRounds(EscortId) <= ReserveRounds() ? "Your officer is preserving the defensive reserve. Change doctrine or take manual control to accept additional risk."
            : _world.ThreatAlive ? "Track established. Delegate release under your reserve policy, or select manual control and engage."
            : "Threat confirmed destroyed. Keep the convoy moving to the exit and preserve remaining rounds.";
        var moments = SelectDecisiveMoments();
        var alerts = WatchAttentionGroupingProjection.Project(Bridge.Session!.WatchQueue.SnapshotVisible())
            .Where(group => group.UnresolvedCount > 0)
            .Select(group => $"{group.Summary} — {group.UnresolvedCount} need attention; focus {group.FocusSubjectId}").ToArray();
        var debrief = IsComplete
            ? $"{_world.Outcome} Objective {(_world.Succeeded ? "met" : "not met")}; friendly rounds expended {spent}; reserve {GetRounds(EscortId)}. Correctly withheld orders are diagnostic only and carry no score penalty."
            : "The debrief will link your decisions to confirmed mission outcomes.";
        return new EscortMissionPresentation(_definition.Title, _definition.Briefing,
            $"Escort convoy to the exit within {_definition.DeadlineSeconds / 60:0.#} minutes; keep it alive.",
            status, guidance, _world.Posture.ToString(), _world.RadarActive ? "Active — fast identification, emissions exposed" : "Passive — slower identification, lower exposure",
            Math.Min(1, _world.ConvoyXNm / _definition.ExitDistanceNm), Math.Max(0, _definition.DeadlineSeconds - SimTime),
            score, IsComplete, _world.Succeeded, moments, alerts, GetRounds(EscortId), _world.ContactState, debrief);
    }

    private IReadOnlyList<EscortDecisiveMoment> SelectDecisiveMoments()
    {
        if (_moments.Count <= 3) return _moments.ToArray();
        var decisive = new List<EscortDecisiveMoment>(3);
        var command = _commandHistory.FirstOrDefault(m => m.Title is "Radar active" or "Radar passive" or "Delegation changed");
        if (command is not null) decisive.Add(command);
        var combat = _moments.FirstOrDefault(m => m.Title == "Combat outcome");
        if (combat is not null) decisive.Add(combat);
        var final = _moments[_moments.Count - 1];
        if (!decisive.Contains(final)) decisive.Add(final);
        return decisive.ToArray();
    }
}
