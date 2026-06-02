namespace ProjectAegis.Sim.Sensors;

/// <summary>Deterministic contact state transition emitted on tick 4 (sensor slice).</summary>
public readonly record struct ContactTransition(
    ulong SimTick,
    double SimTime,
    string ObserverId,
    string ContactId,
    string TargetId,
    ContactLifecycleState PreviousState,
    ContactLifecycleState NewState);