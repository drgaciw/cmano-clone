namespace ProjectAegis.Delegation.Decision;

/// <summary>Effective policy / ROE change row (C1).</summary>
public sealed record PolicyUpdateRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    ulong PolicySnapshotId,
    string Field,
    string PreviousValue,
    string NewValue);