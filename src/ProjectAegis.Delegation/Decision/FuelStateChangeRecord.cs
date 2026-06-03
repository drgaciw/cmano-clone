namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Order-log fuel band transition (logistics GDD / MagazineChange parity).</summary>
public sealed record FuelStateChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    string PreviousState,
    string NewState,
    double RemainingFuelKg);