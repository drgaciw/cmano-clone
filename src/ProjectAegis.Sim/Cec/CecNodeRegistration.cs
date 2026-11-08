namespace ProjectAegis.Sim.Cec;

/// <summary>
/// SWARM-31 / B6a: registration payload for a unit that may participate in CEC mesh.
/// Geometry is degrees lat/lon (same placeholder kinematics band as other Sim evaluators).
/// </summary>
public sealed record CecNodeRegistration(
    string UnitId,
    string SideId,
    bool CecCapable,
    double LatDeg,
    double LonDeg,
    bool IsAlive = true,
    // Public positional contract: retained for construction, deconstruction, and record equality.
    // ReSharper disable once NotAccessedPositionalProperty.Global
    bool IsSwarm = false);
