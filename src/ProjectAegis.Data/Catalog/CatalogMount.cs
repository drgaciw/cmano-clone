namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase A: a weapon host on a platform (sorted by platform_id, mount_id).</summary>
public sealed record CatalogMount(
    string PlatformId,
    string MountId,
    string MountType = "rail",
    double ArcDeg = 360.0,
    int Capacity = 1,
    string ReviewState = CatalogReviewStates.Provisional);
