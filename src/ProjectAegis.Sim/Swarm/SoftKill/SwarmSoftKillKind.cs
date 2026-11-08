namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>SWARM-18 / DRG-107: soft-kill effect category for event log rows.</summary>
public enum SwarmSoftKillKind
{
    Emp = 0,
    Jam = 1,
    ClearJam = 2,
    ClearEmp = 3,
    ModeBlocked = 4,
}
