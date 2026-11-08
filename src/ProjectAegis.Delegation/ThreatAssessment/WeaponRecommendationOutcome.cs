namespace ProjectAegis.Delegation.ThreatAssessment;

/// <summary>
/// Whether a recommended weapon choice is feasible or withheld.
/// Every value is advisory. None authorizes weapons release, enqueues an order, or approves one.
/// </summary>
public enum WeaponRecommendationOutcome
{
    Feasible = 0,
    WithheldByPolicy = 1,
    WithheldByEngage = 2,
    NoRecommendation = 3,
}
