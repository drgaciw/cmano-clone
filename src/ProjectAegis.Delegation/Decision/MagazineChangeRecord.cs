namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Order-log row for magazine consumption (logistics GDD / doc 17).</summary>
public sealed record MagazineChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId ShooterTargetId,
    ulong MountId,
    int Delta,
    string ReasonCode);