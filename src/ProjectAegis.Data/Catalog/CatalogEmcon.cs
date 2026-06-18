namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase B: platform EMCON profile row (sorted by platform_id, condition, emitter_id).</summary>
public sealed record CatalogEmcon(
    string PlatformId,
    string Condition = "silent",
    string EmitterId = "",
    string Posture = "off",
    string ReviewState = CatalogReviewStates.Provisional);