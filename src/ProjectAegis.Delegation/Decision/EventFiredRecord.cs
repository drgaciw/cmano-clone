namespace ProjectAegis.Delegation.Decision;

public sealed record EventFiredRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string EventId,
    string EventCode);