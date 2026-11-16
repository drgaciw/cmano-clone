using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Advice;

/// <summary>
/// Validated AdviceFrame facts accepted by the host explanation pipeline.
/// Opposing-side facts must be tagged and are stripped before prompt construction.
/// </summary>
public sealed record AdviceExplanationFacts(
    string ContactId,
    string Availability,
    double? Confidence,
    string ContactConfidenceLabel,
    string Rationale,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> HardConstraints,
    IReadOnlyList<string> PolicyConstraints,
    IReadOnlyList<string> Alternatives,
    IReadOnlyList<string> MissionPackageFacts,
    IReadOnlyList<string> ResourceCommitments,
    string Fallback,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    IReadOnlyList<AdviceSideTaggedFact> SideTaggedFacts);

/// <summary>A fact that may be hidden from a viewing side.</summary>
public sealed record AdviceSideTaggedFact(
    string Key,
    string Text,
    ViewingSide OwningSide,
    IReadOnlyList<ViewingSide> HiddenFrom);
