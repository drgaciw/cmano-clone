namespace ProjectAegis.Delegation.BdaAssess;

/// <summary>
/// Explicit pending-assessment fact for a target (mirrors engage-assess in-flight input without
/// importing CombatEvents). Caller supplies target id; projection never invents pending rows.
/// </summary>
public sealed record BdaAssessPendingTarget(
  string TargetId,
  ulong SimTick,
  double SimTime,
  ulong CorrelationSequenceId);
