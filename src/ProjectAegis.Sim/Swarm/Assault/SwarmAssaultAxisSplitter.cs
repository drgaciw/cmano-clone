namespace ProjectAegis.Sim.Swarm.Assault;

using ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-17 / DRG-106: pure deterministic multi-axis auto-split planner for Assault mode.
/// Splits logical mass across approach axes against a single HVT when doctrine allows.
/// No per-drone physics outcomes; allocation only.
/// </summary>
public static class SwarmAssaultAxisSplitter
{
    /// <summary>Default requested axis count when callers omit K (must be ≥2 for a split).</summary>
    public const int DefaultAxisCount = 2;

    /// <summary>
    /// Degrees of approach bearing between adjacent axes when fanning around the target bearing.
    /// For K=2 axes lie at base±half step; for K=3 at base−step, base, base+step.
    /// </summary>
    public const double DefaultAxisSpreadDeg = 30.0;

    /// <summary>
    /// Plans axis allocations for a swarm assault.
    /// </summary>
    /// <param name="droneCount">Living logical mass to allocate (shares sum exactly to this when >0).</param>
    /// <param name="axisCount">Requested axis count K (effective K reduced when droneCount < K).</param>
    /// <param name="mode">Operational mode; split only when <see cref="SwarmOperationalMode.Assault"/>.</param>
    /// <param name="seed">Determinism seed (same inputs always produce the same plan).</param>
    /// <param name="doctrineAllowSplit">Doctrine gate; false forces single-axis / no split.</param>
    /// <param name="targetBearingDeg">Optional base approach bearing (degrees); defaults to 0.</param>
    public static SwarmAssaultSplitPlan Plan(
        int droneCount,
        int axisCount,
        SwarmOperationalMode mode,
        ulong seed,
        bool doctrineAllowSplit = true,
        double? targetBearingDeg = null)
    {
        var requested = axisCount;
        var baseBearing = NormalizeBearingDeg(targetBearingDeg ?? 0.0);

        if (droneCount <= 0)
        {
            return new SwarmAssaultSplitPlan(
                SplitApplied: false,
                RequestedAxisCount: requested,
                EffectiveAxisCount: 0,
                Axes: Array.Empty<SwarmAssaultAxisAllocation>());
        }

        var allowSplit =
            mode == SwarmOperationalMode.Assault &&
            doctrineAllowSplit &&
            axisCount >= 2;

        if (!allowSplit)
        {
            return SingleAxisPlan(droneCount, requested, baseBearing, splitApplied: false);
        }

        // Min ≥1 drone per axis: reduce K when mass is thinner than requested axes.
        var effectiveK = Math.Min(axisCount, droneCount);
        if (effectiveK < 2)
        {
            return SingleAxisPlan(droneCount, requested, baseBearing, splitApplied: false);
        }

        var shares = AllocateShares(droneCount, effectiveK, seed);
        var axes = new SwarmAssaultAxisAllocation[effectiveK];
        for (var i = 0; i < effectiveK; i++)
        {
            axes[i] = new SwarmAssaultAxisAllocation(
                AxisIndex: i,
                DroneShare: shares[i],
                ApproachBearingDeg: ApproachBearingForAxis(i, effectiveK, baseBearing));
        }

        return new SwarmAssaultSplitPlan(
            SplitApplied: true,
            RequestedAxisCount: requested,
            EffectiveAxisCount: effectiveK,
            Axes: axes);
    }

    private static SwarmAssaultSplitPlan SingleAxisPlan(
        int droneCount,
        int requestedAxisCount,
        double baseBearing,
        bool splitApplied)
    {
        var axes = new[]
        {
            new SwarmAssaultAxisAllocation(
                AxisIndex: 0,
                DroneShare: droneCount,
                ApproachBearingDeg: baseBearing),
        };

        return new SwarmAssaultSplitPlan(
            SplitApplied: splitApplied,
            RequestedAxisCount: requestedAxisCount,
            EffectiveAxisCount: 1,
            Axes: axes);
    }

    /// <summary>
    /// Distributes droneCount across K axes with floor division + remainder.
    /// Remainder is assigned in a seed-deterministic axis order so unequal shares vary with seed.
    /// Every share is ≥1 and shares sum exactly to droneCount.
    /// </summary>
    private static int[] AllocateShares(int droneCount, int axisCount, ulong seed)
    {
        var shares = new int[axisCount];
        var baseShare = droneCount / axisCount;
        var remainder = droneCount % axisCount;

        for (var i = 0; i < axisCount; i++)
        {
            shares[i] = baseShare;
        }

        // Seed-deterministic order of axes that receive the +1 remainder drones.
        var order = BuildAxisOrder(axisCount, seed);
        for (var r = 0; r < remainder; r++)
        {
            shares[order[r]]++;
        }

        return shares;
    }

    /// <summary>Permutation of [0..K) derived from seed (Fisher–Yates with mixed LCG).</summary>
    private static int[] BuildAxisOrder(int axisCount, ulong seed)
    {
        var order = new int[axisCount];
        for (var i = 0; i < axisCount; i++)
        {
            order[i] = i;
        }

        var state = MixSeed(seed, (ulong)(uint)axisCount);
        for (var i = axisCount - 1; i > 0; i--)
        {
            state = NextUInt64(state);
            var j = (int)(state % (ulong)(i + 1));
            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }

    private static double ApproachBearingForAxis(int axisIndex, int axisCount, double baseBearingDeg)
    {
        if (axisCount <= 1)
        {
            return baseBearingDeg;
        }

        // Center the fan on baseBearing: offsets ..., -1.5, -0.5, +0.5, +1.5, ... or integer mid.
        var mid = (axisCount - 1) / 2.0;
        var offset = (axisIndex - mid) * DefaultAxisSpreadDeg;
        return NormalizeBearingDeg(baseBearingDeg + offset);
    }

    internal static double NormalizeBearingDeg(double bearingDeg)
    {
        var d = bearingDeg % 360.0;
        if (d < 0)
        {
            d += 360.0;
        }

        // Canonicalize -0 to 0.
        return d == 0 ? 0.0 : d;
    }

    private static ulong MixSeed(ulong seed, ulong salt)
    {
        ulong x = seed ^ (salt << 17) ^ 0x9E37_79B9_7F4A_7C15UL;
        x ^= x >> 33;
        x *= 0xff51_afd7_ed55_8ccdUL;
        x ^= x >> 33;
        x *= 0xc4ce_b9fe_1a85_ec53UL;
        x ^= x >> 33;
        return x == 0 ? 0xA5A5_5A5A_A5A5_5A5AUL : x;
    }

    private static ulong NextUInt64(ulong state)
    {
        // SplitMix64 step — deterministic, full-period enough for tiny K.
        ulong z = state + 0x9E37_79B9_7F4A_7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58_476D_1CE4_E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D0_49BB_1331_11EBUL;
        return z ^ (z >> 31);
    }
}
