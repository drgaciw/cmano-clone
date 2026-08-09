namespace ProjectAegis.Sim.Swarm;

using System.Globalization;
using System.Text;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;

/// <summary>
/// SWARM-24 / DRG-91: headless golden replay for swarm orders + integrity deltas.
/// Isolated from Baltic order-log golden 6/6 (does not mutate ReplayGoldenRegressionCatalog).
/// </summary>
public sealed record SwarmReplayResult(
    ulong Seed,
    ulong OrderFingerprint,
    ulong IntegrityHash,
    string CanonicalFingerprint,
    IReadOnlyList<SwarmOrderLogEntry> Orders,
    IReadOnlyList<SwarmIntegrityChange> IntegrityTimeline,
    IReadOnlyDictionary<string, int> FinalDroneCounts);

/// <summary>Deterministic swarm scenario runner + reconstruct path for golden fixtures.</summary>
public static class SwarmReplayHarness
{
    public const string GoldenUnitId = "swarm-golden-1";
    public const string GoldenHostileId = "hostile-aa-1";
    public const ulong GoldenSeed = 42;
    public const string GoldenPlatformId = CatalogSwarmPlatformDefaults.GenericSwarmPlatformId;

    /// <summary>
    /// Canonical Phase A golden: Hold → Move → Attack + point-fire then area-AA integrity hits.
    /// Same seed → byte-stable canonical fingerprint (SWARM-24).
    /// </summary>
    public static SwarmReplayResult RunGoldenScenario(ulong seed = GoldenSeed)
    {
        var simSeed = SimSeed.FromScenario(seed);
        var controller = new SwarmController(simSeed, speedDegPerSecond: 0.05);
        var max = SwarmPerformanceCaps.LogicalMaxDronesPerSwarm;
        controller.Register(
            new SwarmUnitIntegrity(GoldenUnitId, GoldenPlatformId, max, max),
            latDeg: CatalogSwarmPlatformDefaults.GenericLatDeg,
            lonDeg: CatalogSwarmPlatformDefaults.GenericLonDeg);

        controller.IssueHold(GoldenUnitId, simTick: 1, simTime: 1.0);
        controller.IssueMove(
            GoldenUnitId,
            targetLatDeg: CatalogSwarmPlatformDefaults.GenericLatDeg + 0.1,
            targetLonDeg: CatalogSwarmPlatformDefaults.GenericLonDeg + 0.1,
            simTick: 2,
            simTime: 2.0);
        controller.Tick(deltaSeconds: 1.0);
        controller.IssueAttack(
            GoldenUnitId,
            attackTargetUnitId: GoldenHostileId,
            simTick: 3,
            simTime: 3.0,
            targetLatDeg: CatalogSwarmPlatformDefaults.GenericLatDeg + 0.2,
            targetLonDeg: CatalogSwarmPlatformDefaults.GenericLonDeg + 0.05);

        // Integrity-affecting events (authorized path only) — mixed profiles for hard-counter coverage.
        ApplyHit(controller, SwarmAaProfileKind.PointFire, simTick: 4, simTime: 4.0);
        ApplyHit(controller, SwarmAaProfileKind.PointFire, simTick: 5, simTime: 5.0);
        ApplyHit(controller, SwarmAaProfileKind.AreaAa, simTick: 6, simTime: 6.0);
        controller.Tick(deltaSeconds: 1.0);

        return Capture(controller, seed);
    }

    /// <summary>
    /// Reconstruct from recorded orders + integrity timeline onto a fresh controller with same spawn.
    /// </summary>
    public static SwarmReplayResult Replay(
        IReadOnlyList<SwarmOrderLogEntry> orders,
        IReadOnlyList<SwarmIntegrityChange> integrityTimeline,
        ulong seed = GoldenSeed,
        int initialDroneCount = SwarmPerformanceCaps.LogicalMaxDronesPerSwarm,
        int maxDrones = SwarmPerformanceCaps.LogicalMaxDronesPerSwarm)
    {
        var controller = new SwarmController(SimSeed.FromScenario(seed), speedDegPerSecond: 0.05);
        controller.Register(
            new SwarmUnitIntegrity(GoldenUnitId, GoldenPlatformId, initialDroneCount, maxDrones),
            latDeg: CatalogSwarmPlatformDefaults.GenericLatDeg,
            lonDeg: CatalogSwarmPlatformDefaults.GenericLonDeg);

        SwarmController.ReplayOrders(controller, orders);
        SwarmController.ReplayIntegrityTimeline(controller, integrityTimeline);
        return Capture(controller, seed);
    }

    /// <summary>Design-max stress: N concurrent swarms × T ticks of aggregate integrity work.</summary>
    public static SwarmStressResult RunDesignMaxStress(
        ulong seed = 7,
        int concurrentSwarms = SwarmPerformanceCaps.DesignMaxConcurrentSwarms,
        int ticks = SwarmPerformanceCaps.StressScenarioTicks)
    {
        concurrentSwarms = Math.Clamp(concurrentSwarms, 1, SwarmPerformanceCaps.DesignMaxConcurrentSwarms);
        ticks = Math.Max(1, ticks);

        var controller = new SwarmController(SimSeed.FromScenario(seed));
        var max = SwarmPerformanceCaps.LogicalMaxDronesPerSwarm;
        for (var i = 0; i < concurrentSwarms; i++)
        {
            var id = $"swarm-stress-{i:D2}";
            controller.Register(
                new SwarmUnitIntegrity(id, GoldenPlatformId, max, max),
                latDeg: 57.0 + (i * 0.01),
                lonDeg: 20.0 + (i * 0.01));
        }

        var integrityOps = 0;
        var workUnits = 0;
        for (var t = 0; t < ticks; t++)
        {
            var simTick = (ulong)(uint)(t + 1);
            var simTime = t + 1.0;
            foreach (var unitId in EnumerateStressUnitIds(concurrentSwarms))
            {
                // One aggregate integrity op per swarm unit — not per logical drone.
                if (controller.TryApplyIntegrityDamage(
                        unitId,
                        dronesLost: 1,
                        simTick,
                        simTime,
                        reasonCode: SwarmEngagementIntegrityApplier.ReasonPointFire,
                        out _))
                {
                    integrityOps++;
                }

                workUnits += SwarmPerformanceCaps.EngagementWorkUnitsPerPulse(1);
            }

            controller.Tick(deltaSeconds: 1.0);
        }

        var expectedOpsCeiling = concurrentSwarms * ticks;
        return new SwarmStressResult(
            ConcurrentSwarms: concurrentSwarms,
            Ticks: ticks,
            IntegrityOpsApplied: integrityOps,
            EngagementWorkUnits: workUnits,
            ExpectedWorkUnitsCeiling: expectedOpsCeiling,
            LogicalDronesAtStart: concurrentSwarms * max,
            FinalIntegrityHash: controller.ComputeIntegrityTimelineHash());
    }

    public static string FormatCanonicalFingerprint(
        ulong seed,
        ulong orderFp,
        ulong integrityHash,
        IReadOnlyList<SwarmOrderLogEntry> orders,
        IReadOnlyList<SwarmIntegrityChange> integrity)
    {
        var sb = new StringBuilder(512);
        sb.Append("SWARM-REPLAY|seed=").Append(seed.ToString(CultureInfo.InvariantCulture));
        sb.Append("|orders=").Append(orderFp.ToString(CultureInfo.InvariantCulture));
        sb.Append("|integrity=").Append(integrityHash.ToString(CultureInfo.InvariantCulture));
        sb.Append("|orderCount=").Append(orders.Count.ToString(CultureInfo.InvariantCulture));
        sb.Append("|integrityCount=").Append(integrity.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var o in orders)
        {
            sb.Append("|O:")
                .Append(o.SequenceId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(o.SimTick.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(o.UnitId).Append(',')
                .Append(o.Intent.ToString());
            if (o.AttackTargetUnitId is { Length: > 0 } at)
            {
                sb.Append("→").Append(at);
            }
        }

        foreach (var c in integrity)
        {
            sb.Append("|I:")
                .Append(c.SequenceId.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(c.SimTick.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(c.UnitId).Append(',')
                .Append(c.PreviousDroneCount.ToString(CultureInfo.InvariantCulture)).Append("→")
                .Append(c.NewDroneCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(c.ReasonCode);
        }

        return sb.ToString();
    }

    private static void ApplyHit(
        SwarmController controller,
        SwarmAaProfileKind profile,
        ulong simTick,
        double simTime)
    {
        SwarmEngagementIntegrityApplier.TryApplyHit(
            controller,
            GoldenUnitId,
            profile,
            simTick,
            simTime,
            out _);
    }

    private static SwarmReplayResult Capture(SwarmController controller, ulong seed)
    {
        var orders = controller.OrderLog.ToArray();
        var integrity = controller.IntegrityTimeline.ToArray();
        var orderFp = controller.ComputeOrderLogFingerprint();
        var integrityHash = controller.ComputeIntegrityTimelineHash();
        var finals = new SortedDictionary<string, int>(StringComparer.Ordinal);
        // Reconstruct finals from integrity end-state via TryGetIntegrity for registered golden unit.
        if (controller.TryGetIntegrity(GoldenUnitId, out var unit))
        {
            finals[unit.UnitId] = unit.DroneCount;
        }

        var canonical = FormatCanonicalFingerprint(seed, orderFp, integrityHash, orders, integrity);
        return new SwarmReplayResult(
            seed,
            orderFp,
            integrityHash,
            canonical,
            orders,
            integrity,
            finals);
    }

    private static IEnumerable<string> EnumerateStressUnitIds(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return $"swarm-stress-{i:D2}";
        }
    }
}

/// <summary>Outcome of design-max concurrent swarm stress (SWARM-25).</summary>
public sealed record SwarmStressResult(
    int ConcurrentSwarms,
    int Ticks,
    int IntegrityOpsApplied,
    int EngagementWorkUnits,
    int ExpectedWorkUnitsCeiling,
    int LogicalDronesAtStart,
    ulong FinalIntegrityHash);
