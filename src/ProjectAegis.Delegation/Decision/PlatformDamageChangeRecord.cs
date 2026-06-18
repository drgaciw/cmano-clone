namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Order-log platform HP% transition from bounded catalog hot-tick damage apply.</summary>
public sealed record PlatformDamageChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    double PreviousHpPct,
    double NewHpPct,
    string ReasonCode);