namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Versioned AI artifact envelope (schema v1). Cache identity is the hash of all fields.
/// Authored content may additionally carry provenance outside this assembly.
/// </summary>
public sealed record ArtifactEnvelope(
    int SchemaVersion,
    string BuildIdentity,
    string ScenarioId,
    string CatalogRevision,
    string OrderLogFingerprint,
    string ProviderId,
    string ModelId,
    string PromptVersion,
    IReadOnlyList<string> EvidenceIds,
    ViewingSide ViewingSide,
    bool DebriefMode,
    GenerationStatus Status,
    string ContentHash);
