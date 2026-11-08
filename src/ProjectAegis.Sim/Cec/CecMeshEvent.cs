namespace ProjectAegis.Sim.Cec;

/// <summary>
/// Append-only mesh event log row for determinism fingerprinting (SWARM-31 / B6a).
/// </summary>
public sealed record CecMeshEvent(
    ulong SequenceId,
    string UnitId,
    CecMeshEventKind Kind,
    CecMeshState PreviousState,
    CecMeshState NewState);
