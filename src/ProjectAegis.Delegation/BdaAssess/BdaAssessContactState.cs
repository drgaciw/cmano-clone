namespace ProjectAegis.Delegation.BdaAssess;

/// <summary>
/// One per-contact BDA assess row for C2 presentation. Sim-clock only — no selection, hover,
/// camera, or panel visibility.
/// </summary>
public sealed record BdaAssessContactState(
  string ContactId,
  string TargetId,
  string ObserverId,
  BdaAssessStateKind State,
  BdaAssessSourceKind Source,
  ulong SimTick,
  double SimTime,
  ulong CorrelationSequenceId);
