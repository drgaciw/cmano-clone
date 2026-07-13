namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase B: platform propulsion/mobility (sorted by platform_id).</summary>
public sealed record CatalogMobility(
    string PlatformId,
    double MaxSpeedKnots = 0,
    double CruiseSpeedKnots = 0,
    double MaxAltitudeFt = 0,
    double MaxDepthM = 0,
    double FuelCapacity = 0,
    double RangeNm = 0,
    double EnduranceHr = 0,
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string CitationRef = "");