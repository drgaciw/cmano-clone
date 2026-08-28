namespace ProjectAegis.Delegation.BdaAssess;

/// <summary>Presentation-facing BDA assess lifecycle for Combat UX Slice B (DRG-216).</summary>
public enum BdaAssessStateKind
{
  None = 0,
  InProgress = 1,
  Damaged = 2,
  Destroyed = 3,
  Unknown = 4,
}
