using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Input for one AAR assistant invocation.</summary>
public sealed record AarAssistantRequest(
    EvidenceDocument Document,
    EvidenceQuantitativeSummary Quantitative,
    IReadOnlyDictionary<string, string>? AuthoritativeFacts,
    string BuildIdentity,
    string ProviderId = "fake",
    string ModelId = "fake-aar",
    bool Enabled = true,
    string ArtifactFileName = "aar-explain.envelope.json");
