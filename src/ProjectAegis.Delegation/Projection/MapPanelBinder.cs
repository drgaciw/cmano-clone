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
        ScenarioCommsDisplaySettings commsDisplay)
    {
        var rows = new List<MapSymbolDisplayRow>(symbols.Count * 2);
        foreach (var symbol in symbols)
        {
            var style = symbol.Affiliation switch
            {
                "Friendly" when symbol.IsDestroyed => "map-symbol--friendly-dead",
                "Friendly" => "map-symbol--friendly",
                "Hostile" => "map-symbol--hostile",
                _ => "map-symbol--neutral",
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
                AppendGhostRow(rows, symbol, commsDisplay, style);
            }
            else if (commsState == CommsState.Denied)
            {
                style += " map-symbol--frozen";
            }

            rows.Add(new MapSymbolDisplayRow(
                symbol.SymbolId,
                symbol.ShapeGlyph,
                symbol.Label,
                symbol.NormalizedX,
                symbol.NormalizedY,
                style,
                selected,
                false));
        }

        return new MapPanelState(theaterLabel, rows);
    }

    private static void AppendGhostRow(
        List<MapSymbolDisplayRow> rows,
        MapSymbolEntry symbol,
        ScenarioCommsDisplaySettings commsDisplay,
        string liveStyle)
    {
        var ghostX = Clamp01(symbol.NormalizedX - commsDisplay.GhostOffsetX);
        var ghostY = Clamp01(symbol.NormalizedY - commsDisplay.GhostOffsetY);
        var ghostStyle = liveStyle.Replace("map-symbol--selected", string.Empty, StringComparison.Ordinal)
            + " map-symbol--ghost";
        rows.Add(new MapSymbolDisplayRow(
            $"ghost:{symbol.SymbolId}",
            symbol.ShapeGlyph,
            $"{symbol.Label} (lag {commsDisplay.DegradedLagTicks})",
            ghostX,
            ghostY,
            ghostStyle.Trim(),
            false,
            true));
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}