namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase A*: datalink/net type catalog row (sorted by link_id). Feeds doc 19 link model.</summary>
public sealed record CatalogLinkEntry(
    string LinkId,
    string DisplayName = "",
    string LinkType = CatalogLinkTypes.Tactical,
    int LatencyMsNominal = 0);