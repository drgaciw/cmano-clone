namespace ProjectAegis.Delegation.ResourceRank;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Read-only facts for one shooter/weapon candidate.
/// Contract shape aligned with ThreatAssessmentInput (#582) for future composition.
/// </summary>
public sealed record ResourceRankCandidateInput(
    string ContactId,
    string TargetId,
    string ShooterUnitId,
    string WeaponId,
    string WeaponLabel,
    in EngageContext EngageContext,
    EffectivePolicy Policy,
    ResourceRankAvailabilityFacts? Availability = null,
    ResourceRankPosture Posture = ResourceRankPosture.Offensive);

/// <summary>Offensive strike vs defensive intercept posture for ranking labels.</summary>
public enum ResourceRankPosture
{
    Offensive = 0,
    Defensive = 1,
}
