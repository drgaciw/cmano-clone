namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One LOD cluster or passthrough symbol for APP-6 map rendering (REQ-20 Phase N).
/// Clusters expose a representative glyph via existing APP-6 resolution paths.
/// </summary>
public sealed record MapSymbolClusterEntry(
    string ClusterId,
    string RepresentativeSymbolId,
    int Count,
    float NormalizedX,
    float NormalizedY,
    double? Latitude,
    double? Longitude,
    string AffiliationMajority,
    string App6Glyph,
    bool IsCluster);
