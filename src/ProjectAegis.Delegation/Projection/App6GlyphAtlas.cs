namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Atlas-backed APP-6 display resolver. Degrades to unicode glyphs when the atlas is unavailable
/// or a frame id is missing (ADR-007 Phase C MVP).
/// </summary>
public static class App6GlyphAtlas
{
    /// <summary>Display payload for Toolkit map labels (glyph text and/or USS frame class).</summary>
    public sealed record DisplayGlyph(
        string Glyph,
        string? AtlasFrameClass,
        bool UsesAtlasFrame);

    public static DisplayGlyph ResolveDisplay(
        App6MapGlyphResolution resolution,
        IApp6AtlasAvailability? atlas = null)
    {
        var catalog = atlas ?? App6AtlasCatalog.Default;
        if (catalog.IsLoaded && catalog.HasFrame(resolution.UssFrameId))
        {
            return new DisplayGlyph(string.Empty, resolution.UssFrameId, UsesAtlasFrame: true);
        }

        return new DisplayGlyph(resolution.UnicodeGlyph, AtlasFrameClass: null, UsesAtlasFrame: false);
    }

    public static DisplayGlyph ResolveDisplay(string affiliation, bool isDestroyed = false, IApp6AtlasAvailability? atlas = null) =>
        ResolveDisplay(App6Sidc.ResolveMapGlyph(affiliation, isDestroyed), atlas);

    public static DisplayGlyph ResolveDisplayFromSidc(string? sidc, IApp6AtlasAvailability? atlas = null) =>
        ResolveDisplay(App6Sidc.ResolveMapGlyphFromSidc(sidc), atlas);
}