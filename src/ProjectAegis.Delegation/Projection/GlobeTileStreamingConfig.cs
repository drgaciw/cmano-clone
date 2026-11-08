namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Optional WGS84 bounding box for theater tile clip (presentation / streaming hint only).
/// Does not mutate sim. Matches Baltic demo geo used by <see cref="CesiumBillboardProjection"/>.
/// </summary>
public sealed record BalticBbox(
    double MinLat,
    double MaxLat,
    double MinLon,
    double MaxLon);

/// <summary>
/// Headless tile-streaming configuration contract (ADR-007 Phase B / Wave 4 Lane C).
/// No Cesium package dependency; no ion secrets. Hosts apply these fields when package + token present.
/// </summary>
public sealed record GlobeTileStreamingConfig(
    int IonAssetId,
    double MaximumScreenSpaceError,
    bool PreloadAncestors,
    bool PreloadSiblings,
    bool EnableFrustumCulling,
    BalticBbox? BalticBbox = null)
{
    /// <summary>
    /// Well-known Cesium ion asset id for Cesium World Terrain (public catalog; not a secret).
    /// </summary>
    public const int CesiumWorldTerrainAssetId = 1;

    /// <summary>Default Cesium screen-space error (lower = higher quality / more tiles).</summary>
    public const double DefaultMaximumScreenSpaceError = 16.0;

    /// <summary>Baltic demo theater clip (lat 59.5–60.5, lon 24.0–25.5).</summary>
    public static BalticBbox DefaultBalticBbox { get; } = new(
        MinLat: 59.5,
        MaxLat: 60.5,
        MinLon: 24.0,
        MaxLon: 25.5);

    /// <summary>
    /// Product default: World Terrain asset, SSE 16, preload on, frustum culling on, Baltic theater bbox.
    /// </summary>
    public static GlobeTileStreamingConfig Default { get; } = new(
        IonAssetId: CesiumWorldTerrainAssetId,
        MaximumScreenSpaceError: DefaultMaximumScreenSpaceError,
        PreloadAncestors: true,
        PreloadSiblings: true,
        EnableFrustumCulling: true,
        BalticBbox: new BalticBbox(
            MinLat: 59.5,
            MaxLat: 60.5,
            MinLon: 24.0,
            MaxLon: 25.5));
}
