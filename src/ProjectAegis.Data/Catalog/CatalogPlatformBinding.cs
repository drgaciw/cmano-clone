namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 / S22-04: platform catalog metadata parsed from CMO markdown (ship/aircraft sections).
/// Distinct from scenario-position <see cref="CatalogPlatformEntry"/>.
/// Optional core position fields (Lat/Lon/CombatRadius) are applied on approve only when
/// <see cref="ApplyCorePosition"/> is true (PDA / explicit core-field proposals).
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
    string SourceFile = "",
    double LatDeg = 0,
    double LonDeg = 0,
    double CombatRadiusNm = 0,
    bool ApplyCorePosition = false);
