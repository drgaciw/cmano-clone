namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// Pure deterministic linkState rules (SWARM-12). C2 channel only — not CEC mesh.
/// </summary>
public static class SwarmLinkEvaluator
{
    /// <summary>Degrees of lat/lon beyond which link is lost (placeholder kinematics band).</summary>
    public const double DefaultLostRangeDeg = 2.0;

    /// <summary>Degrees of lat/lon beyond which link degrades when host is alive and not jammed.</summary>
    public const double DefaultDegradedRangeDeg = 1.0;

    public static SwarmLinkState Evaluate(
        double? rangeToHostDeg,
        bool hostAlive,
        bool jammed,
        double degradedRangeDeg = DefaultDegradedRangeDeg,
        double lostRangeDeg = DefaultLostRangeDeg)
    {
        if (!hostAlive)
        {
            return SwarmLinkState.Lost;
        }

        if (jammed)
        {
            return SwarmLinkState.Lost;
        }

        if (rangeToHostDeg is not double range)
        {
            // No host geometry — treat as connected for free-flying swarms without host bind.
            return SwarmLinkState.Connected;
        }

        if (range >= lostRangeDeg)
        {
            return SwarmLinkState.Lost;
        }

        if (range >= degradedRangeDeg)
        {
            return SwarmLinkState.Degraded;
        }

        return SwarmLinkState.Connected;
    }

    public static double RangeDeg(double latA, double lonA, double latB, double lonB)
    {
        var dLat = latA - latB;
        var dLon = lonA - lonB;
        return Math.Sqrt((dLat * dLat) + (dLon * dLon));
    }
}
