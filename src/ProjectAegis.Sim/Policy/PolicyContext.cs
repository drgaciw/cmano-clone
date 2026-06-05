namespace ProjectAegis.Sim.Policy;

public readonly record struct PolicyContext(
    ulong UnitId,
    ulong PolicySnapshotId,
    ulong SimTick,
    EffectivePolicy Effective,
    int SalvoSize = 1);