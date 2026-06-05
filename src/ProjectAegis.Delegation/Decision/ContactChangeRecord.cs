namespace ProjectAegis.Delegation.Decision;

/// <summary>Order-log row for sensor contact lifecycle (sensor GDD / order-log-replay).</summary>
public sealed record ContactChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string ObserverId,
    string ContactId,
    string TargetId,
    string PreviousState,
    string NewState);