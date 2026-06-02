namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Post-launch combat outcome row (hit/miss/kill) for replay fingerprint.</summary>
public sealed record EngagementOutcomeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId ShooterTargetId,
    ulong EngagementId,
    string OutcomeCode,
    double PkDraw);