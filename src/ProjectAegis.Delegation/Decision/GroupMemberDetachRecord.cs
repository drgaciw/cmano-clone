namespace ProjectAegis.Delegation.Decision;

using Core;

public sealed record GroupMemberDetachRecord(
    ulong SequenceId,
    double SimTime,
    TargetId GroupId,
    TargetId UnitId);
