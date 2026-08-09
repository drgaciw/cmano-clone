namespace ProjectAegis.Sim.Swarm;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;

/// <summary>
/// SWARM-A2 / DRG-87: aggregate swarm controller — centroid motion, Hold/Move/Attack
/// headless logged intents, authorized integrity damage, deterministic aggregate SoT.
/// No per-drone physics outcomes (SWARM-07). Surface: Sim only (not Unity, not A3 weapons).
/// SWARM-A6: integrity timeline replay + logical caps (DRG-91).
/// SWARM-B1 / DRG-94: operational modes, host bind, linkState (C2 channel only).
/// </summary>
public sealed class SwarmController
{
    /// <summary>Default centroid speed in degrees of lat/lon per sim-second (Phase A placeholder kinematics).</summary>
    public const double DefaultSpeedDegPerSecond = 0.05;

    private readonly SimSeed _seed;
    private readonly Dictionary<string, SwarmRuntimeUnit> _units = new(StringComparer.Ordinal);
    private readonly SwarmOrderLog _orderLog = new();
    private readonly List<SwarmIntegrityChange> _integrityTimeline = new();
    private ulong _integritySequence = 1;
    private readonly List<SwarmModeOrderLogEntry> _modeOrderLog = new();
    private ulong _modeSequence = 1;
    private readonly Dictionary<string, (double Lat, double Lon, bool Alive)> _hosts =
        new(StringComparer.Ordinal);

    public SwarmController(SimSeed seed, double speedDegPerSecond = DefaultSpeedDegPerSecond)
    {
        _seed = seed;
        SpeedDegPerSecond = speedDegPerSecond > 0 ? speedDegPerSecond : DefaultSpeedDegPerSecond;
    }

    public SimSeed Seed => _seed;

    public double SpeedDegPerSecond { get; }

    public IReadOnlyList<SwarmOrderLogEntry> OrderLog => _orderLog.Entries;

    public IReadOnlyList<SwarmIntegrityChange> IntegrityTimeline => _integrityTimeline;

    public IReadOnlyList<SwarmModeOrderLogEntry> ModeOrderLog => _modeOrderLog;

    /// <summary>
    /// Registers a swarm unit from Data-side <see cref="SwarmUnitIntegrity"/> plus spawn centroid.
    /// Logical max/count are clamped to <see cref="SwarmPerformanceCaps.LogicalMaxDronesPerSwarm"/> (SWARM-25).
    /// </summary>
    public void Register(SwarmUnitIntegrity integrity, double latDeg, double lonDeg)
    {
        if (integrity is null)
        {
            throw new ArgumentNullException(nameof(integrity));
        }

        if (string.IsNullOrWhiteSpace(integrity.UnitId))
        {
            throw new ArgumentException("UnitId is required.", nameof(integrity));
        }

        var id = integrity.UnitId.Trim();
        var max = SwarmPerformanceCaps.ClampLogicalMaxDrones(integrity.MaxDrones);
        var count = SwarmPerformanceCaps.ClampDroneCount(integrity.DroneCount, max);
        _units[id] = new SwarmRuntimeUnit(
            id,
            integrity.PlatformId,
            count,
            max,
            latDeg,
            lonDeg);
    }

    public bool Contains(string unitId) =>
        !string.IsNullOrWhiteSpace(unitId) && _units.ContainsKey(unitId.Trim());

    public bool TryGetCentroid(string unitId, out double latDeg, out double lonDeg)
    {
        latDeg = 0;
        lonDeg = 0;
        if (!TryGetUnit(unitId, out var unit))
        {
            return false;
        }

        latDeg = unit.LatDeg;
        lonDeg = unit.LonDeg;
        return true;
    }

    public bool TryGetIntegrity(string unitId, out SwarmUnitIntegrity integrity)
    {
        integrity = default!;
        if (!TryGetUnit(unitId, out var unit))
        {
            return false;
        }

        integrity = new SwarmUnitIntegrity(unit.UnitId, unit.PlatformId, unit.DroneCount, unit.MaxDrones);
        return true;
    }

    public SwarmIntentKind GetIntent(string unitId) =>
        TryGetUnit(unitId, out var unit) ? unit.Intent : SwarmIntentKind.Hold;

    public SwarmOperationalMode GetMode(string unitId) =>
        TryGetUnit(unitId, out var unit) ? unit.Mode : SwarmOperationalMode.Hold;

    public SwarmLinkState GetLinkState(string unitId) =>
        TryGetUnit(unitId, out var unit) ? unit.LinkState : SwarmLinkState.Connected;

    public string? GetHostId(string unitId) =>
        TryGetUnit(unitId, out var unit) ? unit.HostId : null;

    /// <summary>SWARM-11: bind or clear host/mothership for a swarm unit.</summary>
    public void BindHost(string unitId, string? hostId)
    {
        var unit = RequireUnit(unitId);
        unit.HostId = string.IsNullOrWhiteSpace(hostId) ? null : hostId.Trim();
    }

    /// <summary>SWARM-11: publish host geometry/liveness for Screen mode + link evaluation.</summary>
    public void PublishHostState(string hostId, double latDeg, double lonDeg, bool alive = true)
    {
        if (string.IsNullOrWhiteSpace(hostId))
        {
            throw new ArgumentException("Host id is required.", nameof(hostId));
        }

        _hosts[hostId.Trim()] = (latDeg, lonDeg, alive);
    }

    /// <summary>SWARM-10: headless operational mode change (logged).</summary>
    public ulong IssueMode(string unitId, SwarmOperationalMode mode, ulong simTick, double simTime)
    {
        var unit = RequireUnit(unitId);
        EnsureOrdersAccepted(unit);
        unit.Mode = mode;
        var sequenceId = _modeSequence++;
        _modeOrderLog.Add(new SwarmModeOrderLogEntry(
            sequenceId,
            simTick,
            simTime,
            unit.UnitId,
            mode));
        return sequenceId;
    }

    /// <summary>
    /// SWARM-12: recompute and apply linkState from host geometry + jam.
    /// Does not touch CEC mesh state (B6).
    /// </summary>
    public SwarmLinkState RefreshLinkState(string unitId, bool jammed = false)
    {
        var unit = RequireUnit(unitId);
        double? range = null;
        var hostAlive = true;
        if (!string.IsNullOrEmpty(unit.HostId) && _hosts.TryGetValue(unit.HostId, out var host))
        {
            hostAlive = host.Alive;
            range = SwarmLinkEvaluator.RangeDeg(unit.LatDeg, unit.LonDeg, host.Lat, host.Lon);
        }
        else if (!string.IsNullOrEmpty(unit.HostId))
        {
            // Host bound but unknown geometry — treat as degraded until geometry arrives.
            unit.LinkState = jammed ? SwarmLinkState.Lost : SwarmLinkState.Degraded;
            return unit.LinkState;
        }

        unit.LinkState = SwarmLinkEvaluator.Evaluate(range, hostAlive, jammed);
        return unit.LinkState;
    }

    /// <summary>SWARM-12: explicit linkState set (tests / external comms timeline).</summary>
    public void SetLinkState(string unitId, SwarmLinkState linkState)
    {
        RequireUnit(unitId).LinkState = linkState;
    }

    /// <summary>Headless Hold — loiter/station at current centroid (SWARM-03).</summary>
    public ulong IssueHold(string unitId, ulong simTick, double simTime)
    {
        var unit = RequireUnit(unitId);
        EnsureOrdersAccepted(unit);
        unit.Intent = SwarmIntentKind.Hold;
        unit.WaypointLatDeg = null;
        unit.WaypointLonDeg = null;
        unit.AttackTargetUnitId = null;
        return _orderLog.Append(simTick, simTime, unit.UnitId, SwarmIntentKind.Hold);
    }

    /// <summary>Headless Move — plot course toward lat/lon; centroid advances on <see cref="Tick"/>.</summary>
    public ulong IssueMove(
        string unitId,
        double targetLatDeg,
        double targetLonDeg,
        ulong simTick,
        double simTime)
    {
        var unit = RequireUnit(unitId);
        EnsureOrdersAccepted(unit);
        unit.Intent = SwarmIntentKind.Move;
        unit.WaypointLatDeg = targetLatDeg;
        unit.WaypointLonDeg = targetLonDeg;
        unit.AttackTargetUnitId = null;
        return _orderLog.Append(
            simTick,
            simTime,
            unit.UnitId,
            SwarmIntentKind.Move,
            targetLatDeg: targetLatDeg,
            targetLonDeg: targetLonDeg);
    }

    /// <summary>Headless Attack — engage target id; optional centroid waypoint toward target.</summary>
    public ulong IssueAttack(
        string unitId,
        string attackTargetUnitId,
        ulong simTick,
        double simTime,
        double? targetLatDeg = null,
        double? targetLonDeg = null)
    {
        if (string.IsNullOrWhiteSpace(attackTargetUnitId))
        {
            throw new ArgumentException("Attack target unit id is required.", nameof(attackTargetUnitId));
        }

        var unit = RequireUnit(unitId);
        EnsureOrdersAccepted(unit);
        unit.Intent = SwarmIntentKind.Attack;
        unit.AttackTargetUnitId = attackTargetUnitId.Trim();
        unit.WaypointLatDeg = targetLatDeg;
        unit.WaypointLonDeg = targetLonDeg;
        return _orderLog.Append(
            simTick,
            simTime,
            unit.UnitId,
            SwarmIntentKind.Attack,
            targetLatDeg: targetLatDeg,
            targetLonDeg: targetLonDeg,
            attackTargetUnitId: unit.AttackTargetUnitId);
    }

    /// <summary>
    /// Authorized integrity damage only (SWARM-02 / SWARM-07).
    /// Integrity is not writable except through this method — no public field mutation.
    /// </summary>
    public bool TryApplyIntegrityDamage(
        string unitId,
        int dronesLost,
        ulong simTick,
        double simTime,
        string reasonCode,
        out SwarmIntegrityChange change)
    {
        change = default!;
        if (dronesLost <= 0)
        {
            return false;
        }

        if (!TryGetUnit(unitId, out var unit) || unit.DroneCount <= 0)
        {
            return false;
        }

        var previous = unit.DroneCount;
        var lost = Math.Min(dronesLost, previous);
        unit.DroneCount = previous - lost;
        var sequenceId = _integritySequence++;
        change = new SwarmIntegrityChange(
            sequenceId,
            simTick,
            simTime,
            unit.UnitId,
            previous,
            unit.DroneCount,
            lost,
            string.IsNullOrWhiteSpace(reasonCode) ? "unspecified" : reasonCode.Trim());
        _integrityTimeline.Add(change);
        return true;
    }

    /// <summary>
    /// Advances centroid for Move/Attack intents with a waypoint. Hold is stationary.
    /// Kinematics are aggregate only (no per-drone physics).
    /// </summary>
    public void Tick(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        foreach (var unitId in _units.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            var unit = _units[unitId];
            if (unit.DroneCount <= 0)
            {
                continue;
            }

            // SWARM-10/11: Screen mode orbits/gravitates toward bound host when host is known.
            if (unit.Mode == SwarmOperationalMode.Screen &&
                !string.IsNullOrEmpty(unit.HostId) &&
                _hosts.TryGetValue(unit.HostId, out var host) &&
                host.Alive)
            {
                AdvanceCentroid(unit, host.Lat, host.Lon, deltaSeconds);
                continue;
            }

            if (unit.Intent is not (SwarmIntentKind.Move or SwarmIntentKind.Attack))
            {
                continue;
            }

            if (unit.WaypointLatDeg is not double targetLat || unit.WaypointLonDeg is not double targetLon)
            {
                continue;
            }

            AdvanceCentroid(unit, targetLat, targetLon, deltaSeconds);
        }
    }

    /// <summary>
    /// Replays a recorded order log onto a fresh controller with the same registered units.
    /// Used for SWARM-06 replayability tests (intents reconstruct, not full world snapshot).
    /// </summary>
    public static void ReplayOrders(SwarmController target, IReadOnlyList<SwarmOrderLogEntry> orders)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        foreach (var order in orders.OrderBy(o => o.SequenceId))
        {
            switch (order.Intent)
            {
                case SwarmIntentKind.Hold:
                    target.IssueHold(order.UnitId, order.SimTick, order.SimTime);
                    break;
                case SwarmIntentKind.Move:
                    if (order.TargetLatDeg is not double lat || order.TargetLonDeg is not double lon)
                    {
                        throw new InvalidOperationException(
                            $"Move order {order.SequenceId} missing target lat/lon.");
                    }

                    target.IssueMove(order.UnitId, lat, lon, order.SimTick, order.SimTime);
                    break;
                case SwarmIntentKind.Attack:
                    if (string.IsNullOrEmpty(order.AttackTargetUnitId))
                    {
                        throw new InvalidOperationException(
                            $"Attack order {order.SequenceId} missing target unit id.");
                    }

                    target.IssueAttack(
                        order.UnitId,
                        order.AttackTargetUnitId,
                        order.SimTick,
                        order.SimTime,
                        order.TargetLatDeg,
                        order.TargetLonDeg);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown swarm intent {order.Intent}.");
            }
        }
    }

    /// <summary>
    /// Replays integrity-affecting events via the authorized damage API (SWARM-24).
    /// Sequence is by <see cref="SwarmIntegrityChange.SequenceId"/>; sequence ids on the target are reassigned.
    /// </summary>
    public static void ReplayIntegrityTimeline(
        SwarmController target,
        IReadOnlyList<SwarmIntegrityChange> changes)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        foreach (var change in changes.OrderBy(c => c.SequenceId))
        {
            if (!target.TryApplyIntegrityDamage(
                    change.UnitId,
                    change.DronesLost,
                    change.SimTick,
                    change.SimTime,
                    change.ReasonCode,
                    out _))
            {
                // Destroyed or missing unit — stop applying further damage for that unit;
                // continue so later units still reconstruct.
                continue;
            }
        }
    }

    public ulong ComputeOrderLogFingerprint() => _orderLog.ComputeFingerprint();

    /// <summary>Deterministic mix of integrity timeline for same-seed equality (SWARM-07).</summary>
    public ulong ComputeIntegrityTimelineHash()
    {
        ulong mix = SimWorldHash.Fold(_seed.Value);
        foreach (var change in _integrityTimeline)
        {
            mix = SimWorldHash.MixLayer(mix, change.SequenceId, SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, change.SimTick, SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, (ulong)(uint)change.PreviousDroneCount, SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, (ulong)(uint)change.NewDroneCount, SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, HashString(change.UnitId), SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, HashString(change.ReasonCode), SimWorldHash.LayerCombatOutcome);
        }

        // Fold current living counts so end-state participates even without damage events.
        foreach (var unitId in _units.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            var unit = _units[unitId];
            mix = SimWorldHash.MixLayer(mix, HashString(unitId), SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, (ulong)(uint)unit.DroneCount, SimWorldHash.LayerCombatOutcome);
            mix = SimWorldHash.MixLayer(mix, (ulong)(uint)unit.MaxDrones, SimWorldHash.LayerCombatOutcome);
        }

        return mix;
    }

    /// <summary>SWARM-11 stub: host death forces Hold mode + last-order Hold intent when link lost.</summary>
    public void NotifyHostLost(string unitId)
    {
        var unit = RequireUnit(unitId);
        unit.LinkState = SwarmLinkState.Lost;
        unit.Mode = SwarmOperationalMode.Hold;
        unit.Intent = SwarmIntentKind.Hold;
        unit.WaypointLatDeg = null;
        unit.WaypointLonDeg = null;
        if (!string.IsNullOrEmpty(unit.HostId) && _hosts.TryGetValue(unit.HostId, out var host))
        {
            _hosts[unit.HostId] = (host.Lat, host.Lon, Alive: false);
        }
    }

    private static void EnsureOrdersAccepted(SwarmRuntimeUnit unit)
    {
        if (unit.LinkState == SwarmLinkState.Lost)
        {
            throw new InvalidOperationException(
                $"Orders blocked: linkState=lost for swarm '{unit.UnitId}' (SWARM-12).");
        }
    }

    private void AdvanceCentroid(SwarmRuntimeUnit unit, double targetLat, double targetLon, double deltaSeconds)
    {
        var dLat = targetLat - unit.LatDeg;
        var dLon = targetLon - unit.LonDeg;
        var dist = Math.Sqrt((dLat * dLat) + (dLon * dLon));
        if (dist < 1e-12)
        {
            unit.LatDeg = targetLat;
            unit.LonDeg = targetLon;
            return;
        }

        var step = SpeedDegPerSecond * deltaSeconds;
        if (step >= dist)
        {
            unit.LatDeg = targetLat;
            unit.LonDeg = targetLon;
            return;
        }

        var scale = step / dist;
        unit.LatDeg += dLat * scale;
        unit.LonDeg += dLon * scale;
    }

    private bool TryGetUnit(string unitId, out SwarmRuntimeUnit unit)
    {
        unit = null!;
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        return _units.TryGetValue(unitId.Trim(), out unit!);
    }

    private SwarmRuntimeUnit RequireUnit(string unitId)
    {
        if (!TryGetUnit(unitId, out var unit))
        {
            throw new InvalidOperationException($"Unknown swarm unit '{unitId}'.");
        }

        return unit;
    }

    private static ulong HashString(string value)
    {
        ulong h = 14695981039346656037UL;
        foreach (var c in value)
        {
            h ^= c;
            h *= 1099511628211UL;
        }

        return h;
    }

    private sealed class SwarmRuntimeUnit
    {
        public SwarmRuntimeUnit(
            string unitId,
            string platformId,
            int droneCount,
            int maxDrones,
            double latDeg,
            double lonDeg)
        {
            UnitId = unitId;
            PlatformId = platformId;
            DroneCount = droneCount;
            MaxDrones = maxDrones;
            LatDeg = latDeg;
            LonDeg = lonDeg;
            Intent = SwarmIntentKind.Hold;
            Mode = SwarmOperationalMode.Hold;
            LinkState = SwarmLinkState.Connected;
        }

        public string UnitId { get; }
        public string PlatformId { get; }
        public int DroneCount { get; set; }
        public int MaxDrones { get; }
        public double LatDeg { get; set; }
        public double LonDeg { get; set; }
        public SwarmIntentKind Intent { get; set; }
        public SwarmOperationalMode Mode { get; set; }
        public SwarmLinkState LinkState { get; set; }
        public string? HostId { get; set; }
        public double? WaypointLatDeg { get; set; }
        public double? WaypointLonDeg { get; set; }
        public string? AttackTargetUnitId { get; set; }
    }
}
