using ProjectAegis.Ai.Contracts;
using ProjectAegis.Data.PlatformAssistant;

namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>Request to stage an evidence-fed platform design brief (propose only).</summary>
public sealed record CatalogEvidenceProposeRequest(
    string DatabasePath,
    PlatformDesignBrief SeedBrief,
    IReadOnlyList<RetrievedCatalogEvidence> Evidence,
    ViewingSide ViewingSide,
    string ActorType = "agent",
    string ActorId = "catalog-evidence-propose-v1",
    string? Rationale = null,
    IReadOnlyDictionary<string, (string SourceHash, int Revision)>? KnownSourceRevisions = null);
