namespace ProjectAegis.Delegation.Decision;

/// <summary>ADR-003 discriminated union row; <see cref="Payload"/> type depends on <see cref="Kind"/>.</summary>
public sealed record OrderLogEntry(
    ulong SequenceId,
    OrderLogEntryKind Kind,
    double SimTime,
    object Payload)
{
    /// <summary>C1: maps legacy <see cref="DecisionRecord"/> into the unified log.</summary>
    public static OrderLogEntry FromDecisionRecord(
        DecisionRecord record,
        ulong simTick,
        ulong sequenceId = 0)
    {
        _ = simTick;
        return new OrderLogEntry(sequenceId, OrderLogEntryKind.AgentDecision, record.SimTime, record);
    }
}