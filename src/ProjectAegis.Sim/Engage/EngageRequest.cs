namespace ProjectAegis.Sim.Engage;

public readonly record struct EngageRequest(
    ulong ShooterUnitId,
    ulong TargetId,
    ulong MountId,
    ulong SimTick);
