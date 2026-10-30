namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ResourceRank;
using Skills;
using ThreatAssessment;

/// <summary>Availability of a tick-bound advisory read model.</summary>
public enum AdviceAvailability { Available, EvidenceUnavailable, Stale, ModelUnavailable }

/// <summary>Optional snapshot capability for current, authoritative advisory projections.</summary>
public interface IAdviceEvidenceSource
{
    /// <summary>Gets advisory evidence for a contact at the snapshot's current sim time.</summary>
    bool TryGetAdviceEvidence(string contactId, out AdviceEvidenceSource evidence);
}

/// <summary>Current projections supplied by the runtime; no simulation context is reconstructed here.</summary>
public sealed record AdviceEvidenceSource(
    string ContactId,
    double SimTime,
    WeaponRecommendation? ThreatAssessment,
    ResourceRankSnapshot? ResourceRanking,
    IReadOnlyList<string> MissionPackageFacts,
    bool ModelAvailable = true);

/// <summary>Complete non-authoritative explanation for command review.</summary>
public sealed record AdviceFrame(
    string ContactId,
    ulong SimTick,
    double SimTime,
    AdviceAvailability Availability,
    double? Confidence,
    string ContactConfidenceLabel,
    IReadOnlyList<EvidencePointer> Evidence,
    IReadOnlyList<string> Assumptions,
    string Rationale,
    IReadOnlyList<string> HardConstraints,
    IReadOnlyList<string> PolicyConstraints,
    IReadOnlyList<string> Alternatives,
    IReadOnlyList<string> ResourceCommitments,
    IReadOnlyList<string> MissionPackageFacts,
    C2AuthorityProjection? Authority,
    string Fallback,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    string? ThreatUrgencyBand = null,
    double? ThreatUrgencyScore = null,
    string? WeaponFamilyChoice = null,
    bool LowConfidenceAlternatives = false,
    bool WraExhaustionWarn = false,
    string JevModelPin = ThreatAssessmentProjection.JevModelPin)
{
    /// <summary>Creates an explicit fail-closed frame.</summary>
    public static AdviceFrame Unavailable(ulong tick, double time, AdviceAvailability availability, string fallback,
        string contactId = "") => new(contactId, tick, time, availability, null, "UNKNOWN", [], [], "No grounded recommendation available.",
            ["advisory-only:no-authority-or-order"], [], [], [], [], null, fallback, false, false);

    /// <summary>
    /// DRG-320 recommend chrome. Copies confidence, urgency, and weapon-family choice.
    /// When confidence is below <see cref="ThreatAssessmentProjection.LowConfidenceFloor"/>,
    /// alternatives are the other families. Fire and accept flags stay false.
    /// </summary>
    public static AdviceFrame FromWeaponRecommendation(WeaponRecommendation recommendation, ulong tick, double simTime)
    {
        var lowConfidence = recommendation.Confidence < ThreatAssessmentProjection.LowConfidenceFloor
            && recommendation.AlternativeFamilies.Count > 0;
        return new AdviceFrame(
            recommendation.ContactId,
            tick,
            simTime,
            AdviceAvailability.Available,
            recommendation.Confidence,
            "UNKNOWN",
            [],
            recommendation.Assumptions,
            recommendation.StatusLine,
            ["advisory-only:no-authority-or-order"],
            [],
            lowConfidence ? recommendation.AlternativeFamilies : [],
            [],
            [],
            null,
            "manual review of cited current facts",
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            ThreatUrgencyBand: recommendation.ThreatUrgencyBand,
            ThreatUrgencyScore: recommendation.ThreatUrgencyScore,
            WeaponFamilyChoice: recommendation.WeaponFamilyChoice,
            LowConfidenceAlternatives: lowConfidence,
            WraExhaustionWarn: recommendation.WraExhaustionWarn,
            JevModelPin: recommendation.JevModelPin);
    }
}

/// <summary>Callable skill response containing the shared skill envelope and scoped read model.</summary>
public sealed record AdviceSkillResult(SkillEnvelope Skill, AdviceFrame Advice);
