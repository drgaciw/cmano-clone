namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Scenario;

public static class MapPanelBinder
{
    public static MapPanelState Bind(IReadOnlyList<MapSymbolEntry> symbols, string theaterLabel) =>
        Bind(symbols, theaterLabel, selectedUnitId: null, selectedContactId: null, commsState: CommsState.Nominal);

    public static MapPanelState Bind(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        IApp6AtlasAvailability atlas) =>
        Bind(symbols, theaterLabel, null, null, CommsState.Nominal, ScenarioCommsDisplaySettings.Default, atlas);

    public static MapPanelState Bind(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId,
        string? selectedContactId) =>
        Bind(symbols, theaterLabel, selectedUnitId, selectedContactId, CommsState.Nominal);

    public static MapPanelState Bind(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId,
        string? selectedContactId,
        CommsState commsState) =>
        Bind(symbols, theaterLabel, selectedUnitId, selectedContactId, commsState, ScenarioCommsDisplaySettings.Default);

    public static MapPanelState Bind(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId,
        string? selectedContactId,
        CommsState commsState,
        ScenarioCommsDisplaySettings commsDisplay) =>
        Bind(symbols, theaterLabel, selectedUnitId, selectedContactId, commsState, commsDisplay, App6AtlasCatalog.Default);

    public static MapPanelState Bind(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId,
        string? selectedContactId,
        CommsState commsState,
        ScenarioCommsDisplaySettings commsDisplay,
        IApp6AtlasAvailability atlas)
    {
        var rows = new List<MapSymbolDisplayRow>(symbols.Count * 2);
        foreach (var symbol in symbols)
        {
            var style = symbol.Affiliation switch
            {
                "Friendly" when symbol.IsDestroyed => "map-symbol--friendly-dead",
                "Friendly" => "map-symbol--friendly",
                "Hostile" => "map-symbol--hostile",
                "Neutral" => "map-symbol--neutral",
                "Suspect" => "map-symbol--suspect",
                "Pending" => "map-symbol--pending",
                _ => "map-symbol--unknown",
            };
            var selected = (!string.IsNullOrEmpty(selectedUnitId) && symbol.SymbolId == selectedUnitId)
                || (!string.IsNullOrEmpty(selectedContactId) && symbol.SymbolId == selectedContactId);
            if (selected)
            {
                style += " map-symbol--selected";
            }

            if (commsState == CommsState.Degraded && symbol.Affiliation == "Hostile")
            {
                style += " map-symbol--stale";
                AppendGhostRow(rows, symbol, commsDisplay, style, atlas);
            }
            else if (commsState == CommsState.Denied)
            {
                style += " map-symbol--frozen";
            }

            var display = ResolveSymbolDisplay(symbol, atlas);
            var domainModifier = symbol.Domain.HasValue ? App6DomainModifier.Resolve(symbol.Domain.Value) : null;
            rows.Add(new MapSymbolDisplayRow(
                symbol.SymbolId,
                display.Glyph,
                symbol.Label,
                symbol.NormalizedX,
                symbol.NormalizedY,
                style,
                selected,
                false,
                display.AtlasFrameClass,
                display.UsesAtlasFrame,
                domainModifier?.ModifierClass,
                domainModifier?.UnicodeIcon));
        }

        return new MapPanelState(theaterLabel, rows);
    }

    private static void AppendGhostRow(
        List<MapSymbolDisplayRow> rows,
        MapSymbolEntry symbol,
        ScenarioCommsDisplaySettings commsDisplay,
        string liveStyle,
        IApp6AtlasAvailability atlas)
    {
        var ghostX = Clamp01(symbol.NormalizedX - commsDisplay.GhostOffsetX);
        var ghostY = Clamp01(symbol.NormalizedY - commsDisplay.GhostOffsetY);
        var ghostStyle = liveStyle.Replace("map-symbol--selected", string.Empty, StringComparison.Ordinal)
            + " map-symbol--ghost";
        var display = ResolveSymbolDisplay(symbol, atlas);
        rows.Add(new MapSymbolDisplayRow(
            $"ghost:{symbol.SymbolId}",
            display.Glyph,
            $"{symbol.Label} (lag {commsDisplay.DegradedLagTicks})",
            ghostX,
            ghostY,
            ghostStyle.Trim(),
            false,
            true,
            display.AtlasFrameClass,
            display.UsesAtlasFrame));
    }

    private static App6GlyphAtlas.DisplayGlyph ResolveSymbolDisplay(MapSymbolEntry symbol, IApp6AtlasAvailability atlas)
    {
        if (!string.IsNullOrEmpty(symbol.App6UssFrameId))
        {
            return App6GlyphAtlas.ResolveDisplay(
                new App6MapGlyphResolution(symbol.ShapeGlyph, symbol.App6UssFrameId, symbol.App6Sidc ?? App6Sidc.FallbackSidc),
                atlas);
        }

        return App6GlyphAtlas.ResolveDisplay(symbol.Affiliation, symbol.IsDestroyed, atlas);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}