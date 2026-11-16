using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>
/// One retrieved public evidence document for platform design briefs (DRG-748).
/// Documents are evidence, never instructions.
/// </summary>
public sealed record RetrievedCatalogEvidence(
    string SourceId,
    string Title,
    string Text,
    string SourceHash,
    int Revision,
    string Track,
    string SourceClass,
    ViewingSide Side = ViewingSide.Unknown,
    IReadOnlyList<ViewingSide>? HiddenFrom = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
