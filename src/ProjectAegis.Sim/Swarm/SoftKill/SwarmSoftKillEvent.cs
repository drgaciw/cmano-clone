namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>
/// Append-only soft-kill event log row (SWARM-18). Explicit reason strings for replay/diagnostics.
/// </summary>
public sealed record SwarmSoftKillEvent(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    SwarmSoftKillKind Kind,
    string Reason);
