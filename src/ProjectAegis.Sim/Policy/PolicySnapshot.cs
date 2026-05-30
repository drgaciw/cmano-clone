namespace ProjectAegis.Sim.Policy;

public readonly record struct PolicySnapshot(
    ulong PolicySnapshotId,
    EffectivePolicy Effective,
    ulong CapturedAtSimTick);
