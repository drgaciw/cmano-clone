using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Structured AAR pipeline result. Deterministic report is always present.</summary>
public sealed record AarAssistantResult(
    GenerationStatus Status,
    AarDeterministicReport DeterministicReport,
    AarModelDraft? ModelDraft,
    string PromptVersion,
    string? ProviderId,
    string? ModelId,
    string? SkipReason,
    ArtifactEnvelope? Envelope);
