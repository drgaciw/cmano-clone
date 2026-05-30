namespace ProjectAegis.Delegation.Decision;

public sealed record OrderLogEntry(
    ulong SequenceId,
    OrderLogEntryKind Kind,
    double SimTime,
    object Payload);
