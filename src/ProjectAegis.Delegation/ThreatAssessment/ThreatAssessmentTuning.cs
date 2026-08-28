namespace ProjectAegis.Delegation.ThreatAssessment;

/// <summary>
/// Scenario-supplied confidence tuning for threat assessment (not compiled gameplay constants).
/// Callers inject explicit values; <see cref="Default"/> mirrors legacy DRG-212 baseline numerics.
/// </summary>
public sealed record ThreatAssessmentTuning(
    double DlzInZoneConfidence,
    double DlzApproachingConfidence,
    double DlzOutOfZoneConfidence,
    double LowMagazineMultiplier,
    double AutoEngageUnauthorizedMultiplier,
    double WithheldByEngageDlzOutConfidence,
    double WithheldByEngageNoFireControlConfidence,
    double WithheldByEngageDefaultConfidence,
    double WithheldByPolicyConfidence)
{
    /// <summary>Baseline tuning numerically equal to DRG-212 initial projector defaults.</summary>
    public static ThreatAssessmentTuning Default { get; } = new(
        DlzInZoneConfidence: 0.85,
        DlzApproachingConfidence: 0.55,
        DlzOutOfZoneConfidence: 0.25,
        LowMagazineMultiplier: 0.75,
        AutoEngageUnauthorizedMultiplier: 0.9,
        WithheldByEngageDlzOutConfidence: 0.35,
        WithheldByEngageNoFireControlConfidence: 0.2,
        WithheldByEngageDefaultConfidence: 0.1,
        WithheldByPolicyConfidence: 0);
}
