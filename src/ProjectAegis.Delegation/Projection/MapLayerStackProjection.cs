namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure factory for the CMD-28.2 basemap layer stack (UI-local presentation).
/// </summary>
public static class MapLayerStackProjection
{
    /// <summary>
    /// Default ordered stack. All layers visible except DayNight (off by default).
    /// Shortcut hints are presentation-only discovery strings (not input routing).
    /// </summary>
    public static MapLayerStackState DefaultStack()
    {
        return new MapLayerStackState(
        [
            new MapLayerEntry(MapLayerId.Satellite, "Satellite", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.Relief, "Relief", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.Borders, "Borders", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.Terrain, "Terrain", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.Roads, "Roads", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.LandCover, "Land Cover", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.Placenames, "Placenames", IsVisible: true, ShortcutHint: "none"),
            new MapLayerEntry(MapLayerId.DayNight, "Day / Night", IsVisible: false, ShortcutHint: "none"),
        ]);
    }
}
