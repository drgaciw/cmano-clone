namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record EngagementRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId ShooterTargetId,
    ulong EngagementId,
    bool Launched,
    string? AbortReason = null);
