namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase B: platform damage model (sorted by platform_id).</summary>
public sealed record CatalogPlatformDamage(
    string PlatformId,
    double MaxHp = 100,
    double WithdrawThresholdPct = 0,
    int CriticalFlags = 0,
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string CitationRef = "");