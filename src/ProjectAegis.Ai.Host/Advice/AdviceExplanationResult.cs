using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Advice;

/// <summary>Outcome of one AdviceFrame explanation attempt.</summary>
public sealed record AdviceExplanationResult(
    GenerationStatus Status,
    AdviceExplanationContent? Content,
    ArtifactEnvelope? Envelope,
    AiArtifactTransport? Transport,
    string PromptVersion,
    string ProviderId,
    string ModelId,
    string? SkipReason,
    bool DiscardedAsStale);
