namespace ProjectAegis.Sim.Swarm.Formation;

/// <summary>Headless logged formation change (SWARM-16).</summary>
public sealed record SwarmFormationOrderLogEntry(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    SwarmFormation Formation);
