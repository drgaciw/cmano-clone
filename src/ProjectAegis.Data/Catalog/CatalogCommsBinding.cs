namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 Phase A: a platform's comms/datalink fitting (sorted by platform_id, link_id). Feeds doc 19 link model.
/// Carries provenance per req-06 §6.
/// </summary>
public sealed record CatalogCommsBinding(
    string PlatformId,
    string LinkId,
    string Role = "txrx",
    bool SatcomCapable = false,
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string CitationRef = "");
