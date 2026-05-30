namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Policy;

/// <summary>Order-log policy denial entry (doc 17 / ADR-003 MVP).</summary>
public sealed record PolicyDenialRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    AgentId AgentId,
    TargetId TargetId,
    ulong PolicySnapshotId,
    FireAbortReason Reason,
    OrderKind AttemptedKind);
