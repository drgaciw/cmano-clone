namespace ProjectAegis.Sim.Swarm.Assault;

/// <summary>
/// SWARM-17 / DRG-106: one approach-axis share of logical swarm mass for multi-axis assault.
/// Pure allocation — not per-drone physics SoT.
/// </summary>
public sealed record SwarmAssaultAxisAllocation(
    int AxisIndex,
    int DroneShare,
    double ApproachBearingDeg);
