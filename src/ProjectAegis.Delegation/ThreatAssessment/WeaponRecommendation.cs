namespace ProjectAegis.Delegation.ThreatAssessment;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-212: headless weapon recommendation for Combat UX Slice C.
/// Recommendation only — never authorizes weapons release, never enqueues fire, never auto-engages.
/// </summary>
public sealed record WeaponRecommendation(
    string ContactId,
    string TargetId,
    string ShooterUnitId,
    string WeaponId,
    string WeaponLabel,
    ThreatAssessmentPosture Posture,
    WeaponRecommendationOutcome Outcome,
    ThreatRecommendationKind RecommendationKind,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    double Confidence,
    IReadOnlyList<string> Assumptions,
    ThreatRangeAssessment Range,
    ThreatPolicyConstraints PolicyConstraints,
    string? WithheldReasonCode,
    string StatusLine)
{
    /// <summary>Empty sentinel when no contact/weapon facts are supplied.</summary>
    public static WeaponRecommendation Empty { get; } = new(
        ContactId: string.Empty,
        TargetId: string.Empty,
        ShooterUnitId: string.Empty,
        WeaponId: string.Empty,
        WeaponLabel: string.Empty,
        Posture: ThreatAssessmentPosture.Offensive,
        Outcome: WeaponRecommendationOutcome.NoRecommendation,
        RecommendationKind: ThreatRecommendationKind.AdvisoryRecommendation,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        IsAutomaticEngagement: false,
        Confidence: 0,
        Assumptions: Array.Empty<string>(),
        Range: new ThreatRangeAssessment(0, 0, 0, DlzState.Unknown, "DLZ: —", false),
        PolicyConstraints: new ThreatPolicyConstraints(
            RoeLevel.HoldFire,
            EffectivePolicy.DefaultMaxSalvo,
            AutoEngageAuthorized: true,
            ExpendAuthorized: false,
            PolicyAllowsFire: false,
            PolicyAbortCode: null),
        WithheldReasonCode: null,
        StatusLine: "THREAT: —");
}
