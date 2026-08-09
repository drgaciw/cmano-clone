namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-12: C2/order channel health only. Independent of CEC mesh membership (SWARM-31 / B6).
/// </summary>
public enum SwarmLinkState
{
    Connected = 0,
    Degraded = 1,
    Lost = 2,
}
