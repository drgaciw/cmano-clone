namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Read-only APP-6 billboard resolution for Cesium globe markers (ADR-007 Phase B/C, ADR-010).
/// Maps <see cref="MapSymbolEntry"/> symbols to Baltic demo geo; does not mutate sim/catalog state.
/// </summary>
public sealed record CesiumBillboardMarker(
    string SymbolId,
    string Affiliation,
    double Latitude,
    double Longitude,
    string UnicodeGlyph,
    string UssFrameId,
    string Sidc);

/// <summary>Projects tactical map symbols into Cesium billboard markers with APP-6 glyphs/frames.</summary>
public static class CesiumBillboardProjection
{
    // Baltic demo geo shares production theater bounds with TheaterQuickJump (ADR-018 / TR-c2-004).
    private static GeographicBounds Baltic => TheaterQuickJump.BalticBounds;

    /// <summary>Resolve APP-6 glyph/frame from symbol entry; uses SIDC when present, affiliation when missing.</summary>
    public static App6MapGlyphResolution ResolveGlyph(MapSymbolEntry symbol)
    {
        if (!string.IsNullOrWhiteSpace(symbol.App6Sidc))
        {
            return App6Sidc.ResolveMapGlyphFromSidc(symbol.App6Sidc);
        }

        return App6Sidc.ResolveMapGlyph(symbol.Affiliation, symbol.IsDestroyed);
    }

    /// <summary>Project symbols to Cesium billboard markers with Baltic demo geo.</summary>
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
            var (lat, lon) = MapToBalticGeo(symbol, layoutSeed);
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

    private static (double Lat, double Lon) MapToBalticGeo(MapSymbolEntry symbol, int layoutSeed)
    {
        var bounds = Baltic;
        if (symbol.NormalizedX > 0f || symbol.NormalizedY > 0f)
        {
            return (
                bounds.MinLatitude + symbol.NormalizedY * bounds.LatitudeSpan,
                bounds.MinLongitude + symbol.NormalizedX * bounds.LongitudeSpan);
        }

        var (x, y) = MapPictureProjection.Place(symbol.SymbolId, layoutSeed);
        return (
            bounds.MinLatitude + y * bounds.LatitudeSpan,
            bounds.MinLongitude + x * bounds.LongitudeSpan);
    }
}