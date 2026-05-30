namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record GroupMemberDetachRecord(
    ulong SequenceId,
    double SimTime,
    TargetId GroupId,
    TargetId UnitId);

public sealed record GroupMemberRejoinRecord(
    ulong SequenceId,
    double SimTime,
    TargetId GroupId,
    TargetId UnitId);
