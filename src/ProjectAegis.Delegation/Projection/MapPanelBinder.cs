namespace ProjectAegis.Delegation.Projection;

public static class MapPanelBinder
{
    public static MapPanelState Bind(IReadOnlyList<MapSymbolEntry> symbols, string theaterLabel)
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
            rows.Add(new MapSymbolDisplayRow(
                symbol.SymbolId,
                symbol.ShapeGlyph,
                symbol.Label,
                symbol.NormalizedX,
                symbol.NormalizedY,
                style));
        }

        return new MapPanelState(theaterLabel, rows);
    }
}