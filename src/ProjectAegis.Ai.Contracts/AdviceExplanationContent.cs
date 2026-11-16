namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Host-published plain-language explanation for a validated AdviceFrame (Slice 5).
/// Keyed by the skill invocation id; never written into SkillEnvelope or ReplayProvenance.
/// </summary>
public sealed record AdviceExplanationContent(
    string InvocationKey,
    string EvidenceFingerprint,
    ViewingSide ViewingSide,
    string Explanation,
    IReadOnlyList<string> CitedFactKeys,
    string PromptVersion);
