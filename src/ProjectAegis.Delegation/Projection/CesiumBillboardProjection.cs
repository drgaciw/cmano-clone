namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Read-only APP-6 billboard resolution for Cesium globe markers (ADR-007 Phase B/C, ADR-010).
/// Prefer optional WGS84 lat/lon on <see cref="MapSymbolEntry"/> when present; else Baltic demo geo.
/// Does not mutate sim/catalog state. Theater quick-jump is view-only (see <see cref="GlobeViewProjection"/>).
/// </summary>
public sealed record CesiumBillboardMarker(
    string SymbolId,
    string Affiliation,
    double Latitude,
    double Longitude,
    string UnicodeGlyph,
    string UssFrameId,
    string Sidc,
    string? DistanceLabel = null);

/// <summary>Projects tactical map symbols into Cesium billboard markers with APP-6 glyphs/frames.</summary>
public static class CesiumBillboardProjection
{
    private const double BalticLatMin = 59.5;
    private const double BalticLatSpan = 1.0;
    private const double BalticLonMin = 24.0;
    private const double BalticLonSpan = 1.5;
    private const double EarthRadiusMeters = 6_371_000.0;

    /// <summary>Resolve APP-6 glyph/frame from symbol entry; uses SIDC when present, affiliation when missing.</summary>
    public static App6MapGlyphResolution ResolveGlyph(MapSymbolEntry symbol)
    {
        if (!string.IsNullOrWhiteSpace(symbol.App6Sidc))
        {
            return App6Sidc.ResolveMapGlyphFromSidc(symbol.App6Sidc);
        }

        return App6Sidc.ResolveMapGlyph(symbol.Affiliation, symbol.IsDestroyed);
    }

    /// <summary>
    /// Project symbols to Cesium billboard markers.
    /// Uses symbol lat/lon when both present; otherwise Baltic hash / normalized map.
    /// </summary>
    public static IReadOnlyList<CesiumBillboardMarker> Project(
        IReadOnlyList<MapSymbolEntry> symbols,
        int layoutSeed = 7)
    {
        if (symbols.Count == 0)
        {
            return ProjectSeed();
        }

        var markers = new List<CesiumBillboardMarker>(symbols.Count);
        foreach (var symbol in symbols
                     .OrderBy(s => s.Affiliation, StringComparer.Ordinal)
                     .ThenBy(s => s.SymbolId, StringComparer.Ordinal))
        {
            var resolution = ResolveGlyph(symbol);
            var (lat, lon) = ResolveGeo(symbol, layoutSeed);
            markers.Add(new CesiumBillboardMarker(
                symbol.SymbolId,
                symbol.Affiliation,
                lat,
                lon,
                resolution.UnicodeGlyph,
                resolution.UssFrameId,
                resolution.Sidc));
        }

        return markers;
    }

    /// <summary>
    /// Project markers and attach simple LOD distance labels relative to the product camera.
    /// Presentation-only; does not mutate sim. Optional simple haversine range readout.
    /// </summary>
    public static IReadOnlyList<CesiumBillboardMarker> ProjectWithCamera(
        IReadOnlyList<MapSymbolEntry> symbols,
        GlobeCameraState camera,
        int layoutSeed = 7)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        var baseMarkers = Project(symbols, layoutSeed);
        if (baseMarkers.Count == 0)
        {
            return baseMarkers;
        }

        var withDistance = new List<CesiumBillboardMarker>(baseMarkers.Count);
        foreach (var marker in baseMarkers)
        {
            var distanceM = ApproximateGroundDistanceMeters(
                camera.Latitude,
                camera.Longitude,
                marker.Latitude,
                marker.Longitude);
            withDistance.Add(marker with { DistanceLabel = FormatDistanceLabel(distanceM, camera.AltitudeMeters) });
        }

        return withDistance;
    }

    /// <summary>Single friendly seed marker when no map host/symbols available.</summary>
    public static IReadOnlyList<CesiumBillboardMarker> ProjectSeed()
    {
        var resolution = App6Sidc.ResolveMapGlyph("Friendly");
        return
        [
            new CesiumBillboardMarker(
                "seed-friendly",
                "Friendly",
                60.0,
                25.0,
                resolution.UnicodeGlyph,
                resolution.UssFrameId,
                resolution.Sidc),
        ];
    }

    /// <summary>Fixed Baltic demo pair (1 friendly + 1 hostile) for Editor spike when symbols unavailable at runtime.</summary>
    public static IReadOnlyList<CesiumBillboardMarker> ProjectDemoPair()
    {
        var friendly = App6Sidc.ResolveMapGlyph("Friendly");
        var hostile = App6Sidc.ResolveMapGlyph("Hostile");
        return
        [
            new CesiumBillboardMarker(
                "demo-friendly",
                "Friendly",
                60.17,
                24.94,
                friendly.UnicodeGlyph,
                friendly.UssFrameId,
                friendly.Sidc),
            new CesiumBillboardMarker(
                "demo-hostile",
                "Hostile",
                59.95,
                24.50,
                hostile.UnicodeGlyph,
                hostile.UssFrameId,
                hostile.Sidc),
        ];
    }

    /// <summary>
    /// Resolve WGS84 from symbol when both lat/lon are set; else Baltic demo hash placement.
    /// </summary>
    public static (double Lat, double Lon) ResolveGeo(MapSymbolEntry symbol, int layoutSeed)
    {
        if (symbol.Latitude is double lat && symbol.Longitude is double lon)
        {
            return (lat, lon);
        }

        return MapToBalticGeo(symbol, layoutSeed);
    }

    private static (double Lat, double Lon) MapToBalticGeo(MapSymbolEntry symbol, int layoutSeed)
    {
        if (symbol.NormalizedX > 0f || symbol.NormalizedY > 0f)
        {
            return (
                BalticLatMin + symbol.NormalizedY * BalticLatSpan,
                BalticLonMin + symbol.NormalizedX * BalticLonSpan);
        }

        var (x, y) = MapPictureProjection.Place(symbol.SymbolId, layoutSeed);
        return (
            BalticLatMin + y * BalticLatSpan,
            BalticLonMin + x * BalticLonSpan);
    }

    /// <summary>Simple spherical ground distance (meters) for LOD labels — not a nav solution.</summary>
    public static double ApproximateGroundDistanceMeters(
        double lat1Deg,
        double lon1Deg,
        double lat2Deg,
        double lon2Deg)
    {
        var lat1 = lat1Deg * Math.PI / 180.0;
        var lat2 = lat2Deg * Math.PI / 180.0;
        var dLat = (lat2Deg - lat1Deg) * Math.PI / 180.0;
        var dLon = (lon2Deg - lon1Deg) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static string FormatDistanceLabel(double groundDistanceMeters, double cameraAltitudeMeters)
    {
        // Simple LOD: high camera altitude → coarser units; close-in → km.
        if (cameraAltitudeMeters >= 1_500_000.0 || groundDistanceMeters >= 500_000.0)
        {
            var nm = groundDistanceMeters / 1852.0;
            return $"{nm:0} nm";
        }

        if (groundDistanceMeters >= 10_000.0)
        {
            return $"{groundDistanceMeters / 1000.0:0} km";
        }

        return $"{groundDistanceMeters / 1000.0:0.0} km";
    }
}
