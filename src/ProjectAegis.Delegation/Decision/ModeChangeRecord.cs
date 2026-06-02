namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Simulation or control mode transition (C1).</summary>
public sealed record ModeChangeRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string PreviousMode,
    string NewMode,
    TargetId? UnitId = null);