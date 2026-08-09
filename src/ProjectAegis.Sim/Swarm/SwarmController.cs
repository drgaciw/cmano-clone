namespace ProjectAegis.Sim.Swarm;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;

/// <summary>
/// SWARM-A2 / DRG-87: aggregate swarm controller — centroid motion, Hold/Move/Attack
/// headless logged intents, authorized integrity damage, deterministic aggregate SoT.
/// No per-drone physics outcomes (SWARM-07). Surface: Sim only (not Unity, not A3 weapons).
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

    public SwarmController(SimSeed seed, double speedDegPerSecond = DefaultSpeedDegPerSecond)
    {
        _seed = seed;
        SpeedDegPerSecond = speedDegPerSecond > 0 ? speedDegPerSecond : DefaultSpeedDegPerSecond;
    }

    public SimSeed Seed => _seed;

    public double SpeedDegPerSecond { get; }

    public IReadOnlyList<SwarmOrderLogEntry> OrderLog => _orderLog.Entries;

    public IReadOnlyList<SwarmIntegrityChange> IntegrityTimeline => _integrityTimeline;

    /// <summary>
    /// Registers a swarm unit from Data-side <see cref="SwarmUnitIntegrity"/> plus spawn centroid.
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
        _units[id] = new SwarmRuntimeUnit(
            id,
            integrity.PlatformId,
            integrity.DroneCount,
            integrity.MaxDrones,
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

    /// <summary>Headless Hold — loiter/station at current centroid (SWARM-03).</summary>
    public ulong IssueHold(string unitId, ulong simTick, double simTime)
    {
        var unit = RequireUnit(unitId);
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
        unit.Intent = SwarmIntentKind.Move;
        unit.WaypointLatDeg = targetLatDeg;
        unit.WaypointLonDeg = targetLonDeg;
        unit.AttackTargetUnitId = null;
        return _orderLog.Append(
            simTick,
            simTime,
            unit.UnitId,
            SwarmIntentKind.Move,
            targetLatDeg,
            targetLonDeg);
    }

    /// <summary>
    /// Headless Attack — engage target id; optional geometry for centroid approach (SWARM-03).
    /// Engagement damage itself is A3; this only logs intent + optional approach.
    /// </summary>
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
        unit.Intent = SwarmIntentKind.Attack;
        unit.AttackTargetUnitId = attackTargetUnitId.Trim();
        unit.WaypointLatDeg = targetLatDeg;
        unit.WaypointLonDeg = targetLonDeg;
        return _orderLog.Append(
            simTick,
            simTime,
            unit.UnitId,
            SwarmIntentKind.Attack,
            targetLatDeg,
            targetLonDeg,
            unit.AttackTargetUnitId);
    }

    /// <summary>
    /// Authorized integrity damage API (SWARM-02 / SWARM-07).
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
        }

        public string UnitId { get; }
        public string PlatformId { get; }
        public int MaxDrones { get; }
        public int DroneCount { get; set; }
        public double LatDeg { get; set; }
        public double LonDeg { get; set; }
        public SwarmIntentKind Intent { get; set; }
        public double? WaypointLatDeg { get; set; }
        public double? WaypointLonDeg { get; set; }
        public string? AttackTargetUnitId { get; set; }
    }
}
