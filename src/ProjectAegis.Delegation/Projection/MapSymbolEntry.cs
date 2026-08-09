namespace ProjectAegis.Delegation.Projection;

/// <summary>Tactical map symbol for C2 placeholder and product globe (doc 20 / ADR-007).</summary>
/// <remarks>
/// Optional <see cref="Latitude"/> / <see cref="Longitude"/> enable WGS84 product path
/// (Cesium / <see cref="CesiumBillboardProjection"/>). When absent, Baltic hash placement is used.
/// SWARM-A5: optional <see cref="IsSwarm"/> + <see cref="IntegrityLabel"/> for aggregate integrity readout.
/// </remarks>
public sealed record MapSymbolEntry(
    string SymbolId,
    string Affiliation,
    string ShapeGlyph,
    string Label,
    float NormalizedX,
    float NormalizedY,
    bool IsDestroyed,
    string? App6Sidc = null,
    string? App6UssFrameId = null,
    double? Latitude = null,
    double? Longitude = null,
    bool IsSwarm = false,
    string? IntegrityLabel = null);
