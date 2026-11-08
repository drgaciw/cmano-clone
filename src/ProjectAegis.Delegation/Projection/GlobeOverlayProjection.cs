namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// WGS84 envelope ring marker for product globe overlay bind (CMD-21/34, DRG-161).
/// Polyline is a closed great-circle approximation in lat/lon — presentation only.
/// </summary>
public sealed record GlobeEnvelopeRingMarker(
    string UnitId,
    string RingKind,
    string Domain,
    double CenterLatitude,
    double CenterLongitude,
    double RangeNm,
    bool IsSelectedUnit,
    IReadOnlyList<(double Latitude, double Longitude)> Polyline);

/// <summary>
/// WGS84 datalink edge marker for product globe overlay bind (CMD-32, DRG-161).
/// </summary>
public sealed record GlobeDatalinkEdgeMarker(
    string FromUnitId,
    string ToUnitId,
    string LinkType,
    string Status,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude);

/// <summary>
/// Projects already-built tactical overlay DTOs onto WGS84 globe geometry (ADR-007 / ADR-010).
/// Reuses <see cref="CesiumBillboardProjection.ResolveGeo"/> for unit centers — no sim mutation.
/// </summary>
public static class GlobeOverlayProjection
{
    /// <summary>Default ring tessellation segments (deterministic).</summary>
    public const int DefaultRingSegments = 32;

    private const double MetersPerNauticalMile = 1852.0;
    private const double MetersPerDegreeLatitude = 111_320.0;

    /// <summary>
    /// Projects envelope ring entries to WGS84 ring markers with closed polylines.
    /// Skips rings whose unit id cannot be resolved against <paramref name="symbols"/>.
    /// Deterministic order: RingKind then UnitId (ordinal).
    /// </summary>
    public static IReadOnlyList<GlobeEnvelopeRingMarker> ProjectRings(
        IReadOnlyList<EnvelopeRingEntry>? rings,
        IReadOnlyList<MapSymbolEntry> symbols,
        int layoutSeed = 7,
        int ringSegments = DefaultRingSegments)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (rings is null || rings.Count == 0)
        {
            return Array.Empty<GlobeEnvelopeRingMarker>();
        }

        var geoByUnit = BuildSymbolGeoIndex(symbols, layoutSeed);
        var markers = new List<GlobeEnvelopeRingMarker>(rings.Count);
        foreach (var ring in rings.OrderBy(r => r.RingKind, StringComparer.Ordinal)
                     .ThenBy(r => r.UnitId, StringComparer.Ordinal))
        {
            if (ring is null || string.IsNullOrWhiteSpace(ring.UnitId))
            {
                continue;
            }

            if (!geoByUnit.TryGetValue(ring.UnitId, out var center))
            {
                continue;
            }

            if (ring.RangeNm <= 0)
            {
                continue;
            }

            var polyline = BuildRingPolyline(center.Lat, center.Lon, ring.RangeNm, ringSegments);
            markers.Add(new GlobeEnvelopeRingMarker(
                ring.UnitId,
                ring.RingKind,
                ring.Domain,
                center.Lat,
                center.Lon,
                ring.RangeNm,
                ring.IsSelectedUnit,
                polyline));
        }

        return markers;
    }

    /// <summary>
    /// Projects datalink edge entries to WGS84 edge markers.
    /// Skips edges whose endpoints cannot be resolved against <paramref name="symbols"/>.
    /// Deterministic order matches <see cref="DatalinkPictureProjection"/> sort keys.
    /// </summary>
    public static IReadOnlyList<GlobeDatalinkEdgeMarker> ProjectEdges(
        IReadOnlyList<DatalinkEdgeEntry>? edges,
        IReadOnlyList<MapSymbolEntry> symbols,
        int layoutSeed = 7)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (edges is null || edges.Count == 0)
        {
            return Array.Empty<GlobeDatalinkEdgeMarker>();
        }

        var geoByUnit = BuildSymbolGeoIndex(symbols, layoutSeed);
        var markers = new List<GlobeDatalinkEdgeMarker>(edges.Count);
        foreach (var edge in edges
                     .OrderBy(e => e.FromUnitId, StringComparer.Ordinal)
                     .ThenBy(e => e.ToUnitId, StringComparer.Ordinal)
                     .ThenBy(e => e.LinkType, StringComparer.Ordinal)
                     .ThenBy(e => e.Status, StringComparer.Ordinal))
        {
            if (edge is null
                || string.IsNullOrWhiteSpace(edge.FromUnitId)
                || string.IsNullOrWhiteSpace(edge.ToUnitId))
            {
                continue;
            }

            if (!geoByUnit.TryGetValue(edge.FromUnitId, out var from)
                || !geoByUnit.TryGetValue(edge.ToUnitId, out var to))
            {
                continue;
            }

            markers.Add(new GlobeDatalinkEdgeMarker(
                edge.FromUnitId,
                edge.ToUnitId,
                edge.LinkType,
                edge.Status,
                from.Lat,
                from.Lon,
                to.Lat,
                to.Lon));
        }

        return markers;
    }

    /// <summary>
    /// Builds a closed ring polyline around a WGS84 center at nautical-mile radius.
    /// Uses a local tangent-plane approximation — display only, not a nav solution.
    /// </summary>
    public static IReadOnlyList<(double Latitude, double Longitude)> BuildRingPolyline(
        double centerLatitude,
        double centerLongitude,
        double rangeNm,
        int segments = DefaultRingSegments)
    {
        if (rangeNm <= 0 || segments < 3)
        {
            return Array.Empty<(double, double)>();
        }

        var radiusMeters = rangeNm * MetersPerNauticalMile;
        var latRadiusDeg = radiusMeters / MetersPerDegreeLatitude;
        var cosLat = Math.Max(Math.Cos(centerLatitude * Math.PI / 180.0), 0.05);
        var lonRadiusDeg = radiusMeters / (MetersPerDegreeLatitude * cosLat);

        var points = new List<(double, double)>(segments + 1);
        for (var i = 0; i <= segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;
            var lat = centerLatitude + latRadiusDeg * Math.Sin(angle);
            var lon = centerLongitude + lonRadiusDeg * Math.Cos(angle);
            points.Add((lat, lon));
        }

        return points;
    }

    /// <summary>Resolve a unit's WGS84 from symbols when present.</summary>
    public static bool TryResolveUnitGeo(
        string unitId,
        IReadOnlyList<MapSymbolEntry> symbols,
        out double latitude,
        out double longitude,
        int layoutSeed = 7)
    {
        latitude = 0;
        longitude = 0;
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        foreach (var symbol in symbols)
        {
            if (symbol is null || !string.Equals(symbol.SymbolId, unitId, StringComparison.Ordinal))
            {
                continue;
            }

            var (lat, lon) = CesiumBillboardProjection.ResolveGeo(symbol, layoutSeed);
            latitude = lat;
            longitude = lon;
            return true;
        }

        return false;
    }

    private static Dictionary<string, (double Lat, double Lon)> BuildSymbolGeoIndex(
        IReadOnlyList<MapSymbolEntry> symbols,
        int layoutSeed)
    {
        var index = new Dictionary<string, (double Lat, double Lon)>(symbols.Count, StringComparer.Ordinal);
        foreach (var symbol in symbols)
        {
            if (symbol is null || string.IsNullOrWhiteSpace(symbol.SymbolId))
            {
                continue;
            }

            if (index.ContainsKey(symbol.SymbolId))
            {
                continue;
            }

            var (lat, lon) = CesiumBillboardProjection.ResolveGeo(symbol, layoutSeed);
            index[symbol.SymbolId] = (lat, lon);
        }

        return index;
    }
}
