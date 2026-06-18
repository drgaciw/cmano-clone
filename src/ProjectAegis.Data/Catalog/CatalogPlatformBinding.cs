namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 / S22-04: platform catalog metadata parsed from CMO markdown (ship/aircraft sections).
/// Distinct from scenario-position <see cref="CatalogPlatformEntry"/>.
/// </summary>
public sealed record CatalogPlatformBinding(
    string PlatformId,
    string DisplayName = "",
    string Domain = "surface",
    string PlatformClass = "",
    string Nationality = "",
    int GameTechnologyLevel = 0,
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.InterpretedValue,
    string CitationRef = "",
    string SourceFactId = "",
    string ImportBatchId = "",
    string SourceFile = "");