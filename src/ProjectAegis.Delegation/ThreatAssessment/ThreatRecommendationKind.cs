namespace ProjectAegis.Delegation.ThreatAssessment;

/// <summary>
/// DRG-212: advisory classification for threat-assessment output.
/// Only <see cref="AdvisoryRecommendation"/> is emitted — never authorization or fire orders.
/// </summary>
public enum ThreatRecommendationKind
{
    /// <summary>Headless weapon recommendation for UI review — not weapons release.</summary>
    AdvisoryRecommendation = 0,
}
