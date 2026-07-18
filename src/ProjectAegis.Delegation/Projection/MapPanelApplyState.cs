namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for map placeholder panel state (ASSET-009 / S107).
/// Surfaces theater label, symbol counts, and selection for host/tests.
/// </summary>
public static class MapPanelApplyState
{
    public static MapPanelPresentation Apply(MapPanelState? state)
    {
        if (state is null)
        {
            return MapPanelPresentation.Empty;
        }

        var symbols = state.Symbols ?? Array.Empty<MapSymbolDisplayRow>();
        var selected = 0;
        var ghost = 0;
        string? selectedId = null;
        foreach (var s in symbols)
        {
            if (s.IsSelected)
            {
                selected++;
                selectedId ??= s.SymbolId;
            }

            if (s.IsGhost)
            {
                ghost++;
            }
        }

        return new MapPanelPresentation(
            TheaterLabel: state.TheaterLabel ?? string.Empty,
            SymbolCount: symbols.Count,
            SelectedCount: selected,
            GhostCount: ghost,
            SelectedSymbolId: selectedId);
    }

    public static MapPanelPresentation BindAndApply(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId = null,
        string? selectedContactId = null)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        var bound = MapPanelBinder.Bind(symbols, theaterLabel, selectedUnitId, selectedContactId);
        return Apply(bound);
    }
}

public sealed record MapPanelPresentation(
    string TheaterLabel,
    int SymbolCount,
    int SelectedCount,
    int GhostCount,
    string? SelectedSymbolId)
{
    public static MapPanelPresentation Empty { get; } = new(string.Empty, 0, 0, 0, null);
}
