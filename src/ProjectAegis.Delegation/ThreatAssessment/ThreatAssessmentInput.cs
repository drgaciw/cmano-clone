namespace ProjectAegis.Delegation.ThreatAssessment;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>Read-only facts for headless threat assessment (projection input).</summary>
public sealed record ThreatAssessmentInput(
    string ContactId,
    string TargetId,
    string ShooterUnitId,
    string WeaponId,
    string WeaponLabel,
    in EngageContext EngageContext,
    EffectivePolicy Policy,
    ThreatAssessmentPosture Posture = ThreatAssessmentPosture.Offensive);

/// <summary>Offensive strike vs defensive intercept posture for recommendation labeling.</summary>
public enum ThreatAssessmentPosture
{
    Offensive = 0,
    Defensive = 1,
}
