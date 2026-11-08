namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// Headless logged swarm intent row (SWARM-06 / ADR-010).
/// Sim-local order path for Phase A — Delegation <c>IOrderLog</c> bridge is DRG-91.
/// </summary>
public sealed record SwarmOrderLogEntry(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    SwarmIntentKind Intent,
    double? TargetLatDeg = null,
    double? TargetLonDeg = null,
    string? AttackTargetUnitId = null);
