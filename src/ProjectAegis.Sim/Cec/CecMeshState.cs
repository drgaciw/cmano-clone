namespace ProjectAegis.Sim.Cec;

/// <summary>
/// SWARM-31 / B6a: CEC mesh membership health. Independent of C2
/// <c>SwarmLinkState</c> — never coupled to host/order channel.
/// </summary>
public enum CecMeshState
{
    /// <summary>In mesh with at least one same-side CEC peer within connected range.</summary>
    InMesh = 0,

    /// <summary>Peer only within degraded band (beyond connected, within degraded range).</summary>
    Degraded = 1,

    /// <summary>Not mesh-connected (non-CEC, dead, jammed, out of range, or no peer).</summary>
    OutOfMesh = 2,
}
