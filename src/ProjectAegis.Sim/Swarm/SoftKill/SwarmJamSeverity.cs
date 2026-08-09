namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>
/// SWARM-18 jam severity → linkState mapping (C2 channel only; not CEC mesh).
/// </summary>
public enum SwarmJamSeverity
{
    /// <summary>No jam; link may be restored to Connected.</summary>
    None = 0,

    /// <summary>Moderate jam → <see cref="SwarmLinkState.Degraded"/>.</summary>
    Degraded = 1,

    /// <summary>High severity jam → <see cref="SwarmLinkState.Lost"/>.</summary>
    Lost = 2,
}
