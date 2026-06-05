namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

public sealed record EngagementRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId ShooterTargetId,
    ulong EngagementId,
    bool Launched,
    string? AbortReasonCode = null)
{
    /// <summary>Alias for <see cref="AbortReasonCode"/> (legacy tests).</summary>
    public string? AbortReason => AbortReasonCode;
}
