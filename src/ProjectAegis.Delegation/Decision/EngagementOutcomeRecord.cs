namespace ProjectAegis.Delegation.Decision;

using Core;

/// <summary>Post-launch combat outcome row (hit/miss/kill) for replay fingerprint.</summary>
public sealed record EngagementOutcomeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId ShooterTargetId,
    TargetId VictimTargetId,
    ulong EngagementId,
    string OutcomeCode,
    double PkDraw);