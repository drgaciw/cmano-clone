namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Human-issued order row (C1 / order-log-replay).</summary>
public sealed record PlayerOrderRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    TargetId UnitId,
    OrderKind Kind,
    string Source = "player",
    ulong ExecuteSimTick = 0)
{
    public ulong ResolvedExecuteSimTick => ExecuteSimTick == 0 ? SimTick : ExecuteSimTick;
}