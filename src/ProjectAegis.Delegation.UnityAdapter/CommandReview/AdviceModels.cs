namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ProjectAegis.Delegation.ResourceRank;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.ThreatAssessment;

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
    bool IsFireOrder)
{
    /// <summary>Creates an explicit fail-closed frame.</summary>
    public static AdviceFrame Unavailable(ulong tick, double time, AdviceAvailability availability, string fallback,
        string contactId = "") => new(contactId, tick, time, availability, null, "UNKNOWN", [], [], "No grounded recommendation available.",
            ["advisory-only:no-authority-or-order"], [], [], [], [], null, fallback, false, false);
}

/// <summary>Callable skill response containing the shared skill envelope and scoped read model.</summary>
public sealed record AdviceSkillResult(SkillEnvelope Skill, AdviceFrame Advice);
