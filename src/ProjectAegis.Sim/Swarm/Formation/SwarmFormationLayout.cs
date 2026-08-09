namespace ProjectAegis.Sim.Swarm.Formation;

/// <summary>
/// Pure deterministic soft member-layout offsets for SWARM-16 / DRG-105.
/// Offsets are cosmetic (degrees lat/lon from centroid) — not engagement SoT.
/// </summary>
public static class SwarmFormationLayout
{
    /// <summary>Nominal spacing between members in degrees of lat/lon (placeholder band).</summary>
    public const double DefaultSpacingDeg = 0.01;

    /// <summary>Cloud scatter radius in degrees.</summary>
    public const double DefaultCloudRadiusDeg = 0.04;

    /// <summary>Orbit ring radius in degrees.</summary>
    public const double DefaultOrbitRadiusDeg = 0.03;

    /// <summary>
    /// When host bearing is present, Orbit offsets are shifted this fraction of the orbit
    /// radius toward the host (soft bias, not a hard constraint).
    /// </summary>
    public const double OrbitHostBiasFraction = 0.35;

    /// <summary>
    /// Computes deterministic (dx, dy) offsets in degrees for each logical member.
    /// <paramref name="hostBearingRad"/> is optional bearing from swarm toward host
    /// (radians, atan2(dLon, dLat) convention). Used by Wall/Spear/Orbit when present.
    /// </summary>
    public static IReadOnlyList<(double DxDeg, double DyDeg)> ComputeOffsets(
        SwarmFormation formation,
        int droneCount,
        ulong seed,
        double? hostBearingRad = null)
    {
        if (droneCount <= 0)
        {
            return Array.Empty<(double DxDeg, double DyDeg)>();
        }

        return formation switch
        {
            SwarmFormation.Wall => Wall(droneCount, hostBearingRad),
            SwarmFormation.Spear => Spear(droneCount, hostBearingRad),
            SwarmFormation.Orbit => Orbit(droneCount, hostBearingRad),
            _ => Cloud(droneCount, seed),
        };
    }

    private static IReadOnlyList<(double DxDeg, double DyDeg)> Cloud(int droneCount, ulong seed)
    {
        var result = new (double DxDeg, double DyDeg)[droneCount];
        var state = MixSeed(seed, (ulong)(uint)droneCount);
        for (var i = 0; i < droneCount; i++)
        {
            state = Lcg(state);
            var u1 = UnitDouble(state);
            state = Lcg(state);
            var u2 = UnitDouble(state);
            // Uniform disk: r = R * sqrt(u1), theta = 2π u2
            var r = DefaultCloudRadiusDeg * Math.Sqrt(u1);
            var theta = 2.0 * Math.PI * u2;
            result[i] = (r * Math.Cos(theta), r * Math.Sin(theta));
        }

        return result;
    }

    private static IReadOnlyList<(double DxDeg, double DyDeg)> Wall(int droneCount, double? hostBearingRad)
    {
        // Wall is perpendicular to host bearing (or east-west when unbound).
        var bearing = hostBearingRad ?? 0.0;
        var alongLat = -Math.Sin(bearing); // perpendicular
        var alongLon = Math.Cos(bearing);
        return Line(droneCount, alongLat, alongLon, DefaultSpacingDeg);
    }

    private static IReadOnlyList<(double DxDeg, double DyDeg)> Spear(int droneCount, double? hostBearingRad)
    {
        // Spear points along host bearing (or due north when unbound).
        var bearing = hostBearingRad ?? 0.0;
        var alongLat = Math.Cos(bearing);
        var alongLon = Math.Sin(bearing);
        return Line(droneCount, alongLat, alongLon, DefaultSpacingDeg);
    }

    private static IReadOnlyList<(double DxDeg, double DyDeg)> Orbit(int droneCount, double? hostBearingRad)
    {
        var result = new (double DxDeg, double DyDeg)[droneCount];
        var startAngle = hostBearingRad ?? 0.0;
        var biasLat = 0.0;
        var biasLon = 0.0;
        if (hostBearingRad is double bearing)
        {
            var bias = DefaultOrbitRadiusDeg * OrbitHostBiasFraction;
            biasLat = bias * Math.Cos(bearing);
            biasLon = bias * Math.Sin(bearing);
        }

        for (var i = 0; i < droneCount; i++)
        {
            var angle = startAngle + (2.0 * Math.PI * i / droneCount);
            result[i] = (
                (DefaultOrbitRadiusDeg * Math.Cos(angle)) + biasLat,
                (DefaultOrbitRadiusDeg * Math.Sin(angle)) + biasLon);
        }

        return result;
    }

    private static IReadOnlyList<(double DxDeg, double DyDeg)> Line(
        int droneCount,
        double dirLat,
        double dirLon,
        double spacing)
    {
        var result = new (double DxDeg, double DyDeg)[droneCount];
        var mid = (droneCount - 1) * 0.5;
        for (var i = 0; i < droneCount; i++)
        {
            var t = (i - mid) * spacing;
            result[i] = (t * dirLat, t * dirLon);
        }

        return result;
    }

    private static ulong MixSeed(ulong seed, ulong salt)
    {
        unchecked
        {
            var x = seed ^ (salt * 0x9E3779B97F4A7C15UL);
            x ^= x >> 30;
            x *= 0xBF58476D1CE4E5B9UL;
            x ^= x >> 27;
            x *= 0x94D049BB133111EBUL;
            x ^= x >> 31;
            return x == 0 ? 0xA5A5A5A5A5A5A5A5UL : x;
        }
    }

    private static ulong Lcg(ulong state) =>
        unchecked((state * 6364136223846793005UL) + 1442695040888963407UL);

    private static double UnitDouble(ulong state) =>
        (state >> 11) * (1.0 / (1UL << 53));
}
