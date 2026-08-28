namespace ProjectAegis.Delegation.ThreatAssessment;

/// <summary>Whether a recommended weapon choice is feasible or withheld.</summary>
public enum WeaponRecommendationOutcome
{
    Feasible = 0,
    WithheldByPolicy = 1,
    WithheldByEngage = 2,
    NoRecommendation = 3,
}
