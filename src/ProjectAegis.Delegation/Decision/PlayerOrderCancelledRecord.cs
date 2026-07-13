namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Human-initiated cancellation of a queued/plotted order before it executes
/// (req 20 rev 2 §Order lifecycle, TR-c2-006; <c>PlayerOrderCancelled</c>). Logged as a deterministic
/// intent (doc 17) — the <see cref="OrderLifecycleProjection"/> maps it to
/// <c>OrderLifecycleState.Aborted</c>. Additive: only ever appended when a player actually cancels.</summary>
public sealed record PlayerOrderCancelledRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    OrderKind Kind,
    ulong CancelledExecuteSimTick = 0,
    string Source = "player");
