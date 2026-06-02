namespace ProjectAegis.Delegation.Decision;

public sealed record MissionTransitionRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string EventId,
    string PhaseCode);