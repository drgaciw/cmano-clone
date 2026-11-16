using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Advice;

/// <summary>
/// Input for one AdviceFrame explanation. <see cref="GetCurrentKeys"/> is sampled after the
/// model returns; a mismatch discards the late/stale response without publishing.
/// </summary>
public sealed record AdviceExplanationRequest(
    string InvocationKey,
    string EvidenceFingerprint,
    ViewingSide ViewingSide,
    AdviceExplanationFacts Facts,
    string BuildIdentity,
    string ScenarioId = "advice",
    string CatalogRevision = "n/a",
    string OrderLogFingerprint = "n/a",
    string ProviderId = "fake",
    string ModelId = "fake-advice",
    bool Enabled = true,
    string ArtifactFileName = "advice-explain.envelope.json",
    Func<AdviceStaleCheck>? GetCurrentKeys = null);

/// <summary>Current selection/evidence keys used for stale discard.</summary>
public readonly record struct AdviceStaleCheck(string InvocationKey, string EvidenceFingerprint);
