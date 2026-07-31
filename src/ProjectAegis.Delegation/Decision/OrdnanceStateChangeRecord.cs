namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Order-log ordnance band transition (Shotgun / Winchester) — magazine counterpart to
/// <see cref="FuelStateChangeRecord"/> (Joker / Bingo).
/// </summary>
public sealed record OrdnanceStateChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    string PreviousState,
    string NewState,
    int RoundsRemaining);
