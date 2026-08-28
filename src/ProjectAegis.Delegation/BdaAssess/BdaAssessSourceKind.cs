namespace ProjectAegis.Delegation.BdaAssess;

/// <summary>Sim-authored provenance for a BDA assess row. Never UI selection or chrome.</summary>
public enum BdaAssessSourceKind
{
  None = 0,
  PlatformDamage = 1,
  EngagementOutcome = 2,
  PendingEngagement = 3,
  ContactLifecycle = 4,
}
