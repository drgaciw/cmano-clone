namespace ProjectAegis.Sim.Swarm.Expend;

/// <summary>Logged expend/kamikaze pulse order (SWARM-19). Separate from Move/Attack intent log.</summary>
public sealed record SwarmExpendOrderLogEntry(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    int DronesRequested,
    int DronesExpended,
    string? TargetUnitId);
