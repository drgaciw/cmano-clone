namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Comms;

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
        CommsState commsState)
    {
        var rows = new List<MapSymbolDisplayRow>(symbols.Count);
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
                selected));
        }

        return new MapPanelState(theaterLabel, rows);
    }
}