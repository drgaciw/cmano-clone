namespace ProjectAegis.Delegation.Decision;

using Core;

public sealed record ControllerChangeRecord(
    ulong SequenceId,
    double SimTime,
    TargetId TargetId,
    string PreviousKind,
    string NewKind,
    AgentId? AgentId = null);
