namespace ProjectAegis.Sim.Swarm;

/// <summary>Headless logged operational-mode change (SWARM-10).</summary>
public sealed record SwarmModeOrderLogEntry(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    SwarmOperationalMode Mode);
