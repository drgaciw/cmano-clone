namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Restyles a projected map picture for a symbology profile (S126-04/05).
/// Pure function over immutable <see cref="MapSymbolEntry"/> rows: only glyph, SIDC and
/// frame id change; identity, affiliation, label, pose and destroyed state are preserved.
/// </summary>
public static class SymbologyProjection
{
    /// <summary>
    /// Applies <paramref name="profile"/> to <paramref name="symbols"/>.
    /// <paramref name="observedSymbolKeys"/> maps symbol id → canonical key known to the observer;
    /// absent ids render as <see cref="SymbolKeyRegistry.GenericUnknown"/>.
    /// </summary>
    public static IReadOnlyList<MapSymbolEntry> Apply(
        IReadOnlyList<MapSymbolEntry> symbols,
        SymbologyProfile profile,
        IReadOnlyDictionary<string, string>? observedSymbolKeys = null)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        if (!SymbologyProfileCatalog.IsSelectable(profile))
        {
            throw new ArgumentException(SymbologyProfileCatalog.CivilianPendingReason, nameof(profile));
        }

        var styled = new MapSymbolEntry[symbols.Count];
        for (var i = 0; i < symbols.Count; i++)
        {
            styled[i] = profile == SymbologyProfile.MilitaryTactical
                ? ApplyMilitary(symbols[i], observedSymbolKeys)
                : ApplyLegacy(symbols[i]);
        }

        return styled;
    }

    private static MapSymbolEntry ApplyLegacy(MapSymbolEntry symbol)
    {
        var resolution = App6Sidc.ResolveMapGlyph(symbol.Affiliation, symbol.IsDestroyed);
        return symbol with
        {
            ShapeGlyph = resolution.UnicodeGlyph,
            App6Sidc = resolution.Sidc,
            App6UssFrameId = resolution.UssFrameId,
        };
    }

    private static MapSymbolEntry ApplyMilitary(
        MapSymbolEntry symbol,
        IReadOnlyDictionary<string, string>? observedSymbolKeys)
    {
        string? observedKey = null;
        observedSymbolKeys?.TryGetValue(symbol.SymbolId, out observedKey);
        var glyph = MilitarySymbology.Resolve(symbol.Affiliation, observedKey, symbol.IsDestroyed);
        return symbol with
        {
            ShapeGlyph = $"{glyph.UnicodeGlyph} {glyph.IconText}",
            App6Sidc = glyph.Sidc,
            App6UssFrameId = glyph.UssFrameId,
        };
    }
}
