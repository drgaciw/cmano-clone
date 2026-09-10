namespace ProjectAegis.Delegation.Decision;

using Core;

public sealed record GroupMemberRejoinRecord(
    ulong SequenceId,
    double SimTime,
    TargetId GroupId,
    TargetId UnitId);
