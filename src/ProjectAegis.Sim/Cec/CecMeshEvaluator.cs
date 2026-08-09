namespace ProjectAegis.Sim.Cec;

/// <summary>
/// Pure deterministic CEC mesh membership rules (SWARM-31 mesh half / B6a).
/// Independent of C2 linkState — does not reference Swarm types.
/// </summary>
public static class CecMeshEvaluator
{
    /// <summary>Degrees of lat/lon within which a same-side CEC peer yields <see cref="CecMeshState.InMesh"/>.</summary>
    public const double DefaultConnectedRangeDeg = 2.0;

    /// <summary>Degrees of lat/lon within which a peer yields <see cref="CecMeshState.Degraded"/> (beyond connected).</summary>
    public const double DefaultDegradedRangeDeg = 4.0;

    /// <summary>
    /// Evaluate mesh state for one node given best same-side CEC peer range and environmental flags.
    /// Non-capable, jammed, dead, or peerless nodes are always <see cref="CecMeshState.OutOfMesh"/>.
    /// </summary>
    public static CecMeshState EvaluateMeshState(
        bool cecCapable,
        bool hasPeerInRange,
        double? bestPeerRangeDeg,
        bool jammed,
        bool alive,
        double connectedRangeDeg = DefaultConnectedRangeDeg,
        double degradedRangeDeg = DefaultDegradedRangeDeg)
    {
        if (!cecCapable || !alive || jammed)
        {
            return CecMeshState.OutOfMesh;
        }

        if (!hasPeerInRange || bestPeerRangeDeg is not double range)
        {
            return CecMeshState.OutOfMesh;
        }

        if (range <= connectedRangeDeg)
        {
            return CecMeshState.InMesh;
        }

        if (range <= degradedRangeDeg)
        {
            return CecMeshState.Degraded;
        }

        return CecMeshState.OutOfMesh;
    }

    /// <summary>Euclidean lat/lon placeholder range (degrees), matching other Sim kinematics bands.</summary>
    public static double RangeDeg(double latA, double lonA, double latB, double lonB)
    {
        var dLat = latA - latB;
        var dLon = lonA - lonB;
        return Math.Sqrt((dLat * dLat) + (dLon * dLon));
    }
}
