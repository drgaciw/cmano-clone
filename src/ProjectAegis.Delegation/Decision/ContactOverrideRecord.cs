namespace ProjectAegis.Delegation.Decision;

/// <summary>Operator contact override action kind (DRG-272 / C2-22).</summary>
public enum ContactOverrideAction
{
    Reclassify = 0,
    Drop = 1,
}

/// <summary>
/// Pure order-log entry recording a player operator contact override (reclassify or drop)
/// without mutating sensor truth or catalog databases (DRG-272 / C2-22).
/// </summary>
public sealed record ContactOverrideRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string ContactId,
    ContactOverrideAction Action,
    string PreviousValue,
    string NewValue);
