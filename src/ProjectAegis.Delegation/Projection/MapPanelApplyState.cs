namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for map placeholder panel state (ASSET-009 / S107).
/// Surfaces theater label, symbol counts, selection, and optional overlay counts (CMD-21/32/34).
/// </summary>
public static class MapPanelApplyState
{
    public static MapPanelPresentation Apply(MapPanelState? state) =>
        Apply(state, envelopeRings: null, datalinkEdges: null);

    /// <summary>
    /// Apply map state with optional envelope ring / datalink edge overlays (CMD-21/32/34).
    /// Overlay lists are count-only for presentation; null or empty yields zero counts.
    /// </summary>
    public static MapPanelPresentation Apply(
        MapPanelState? state,
        IReadOnlyList<EnvelopeRingEntry>? envelopeRings,
        IReadOnlyList<DatalinkEdgeEntry>? datalinkEdges = null)
    {
        if (state is null)
        {
            return MapPanelPresentation.Empty with
            {
                EnvelopeRingCount = CountNonNull(envelopeRings),
                DatalinkEdgeCount = CountNonNull(datalinkEdges),
            };
        }

        var symbols = state.Symbols ?? Array.Empty<MapSymbolDisplayRow>();
        var selected = 0;
        var ghost = 0;
        string? selectedId = null;
        foreach (var s in symbols)
        {
            // Apply is public and takes host-supplied state; a null element would
            // otherwise throw on s.IsSelected.
            if (s is null)
            {
                continue;
            }

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
            SelectedSymbolId: selectedId,
            EnvelopeRingCount: CountNonNull(envelopeRings),
            DatalinkEdgeCount: CountNonNull(datalinkEdges));
    }

    public static MapPanelPresentation BindAndApply(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId = null,
        string? selectedContactId = null) =>
        BindAndApply(symbols, theaterLabel, selectedUnitId, selectedContactId, null, null);

    /// <summary>
    /// Bind symbols then apply with optional overlay lists (CMD-21/32/34).
    /// Existing call sites without overlays remain unchanged.
    /// </summary>
    public static MapPanelPresentation BindAndApply(
        IReadOnlyList<MapSymbolEntry> symbols,
        string theaterLabel,
        string? selectedUnitId,
        string? selectedContactId,
        IReadOnlyList<EnvelopeRingEntry>? envelopeRings,
        IReadOnlyList<DatalinkEdgeEntry>? datalinkEdges = null)
    {
        if (symbols is null)
        {
            throw new ArgumentNullException(nameof(symbols));
        }

        var bound = MapPanelBinder.Bind(symbols, theaterLabel, selectedUnitId, selectedContactId);
        return Apply(bound, envelopeRings, datalinkEdges);
    }

    private static int CountNonNull<T>(IReadOnlyList<T>? items)
    {
        if (items is null || items.Count == 0)
        {
            return 0;
        }

        var count = 0;
        foreach (var item in items)
        {
            if (item is not null)
            {
                count++;
            }
        }

        return count;
    }
}

public sealed record MapPanelPresentation(
    string TheaterLabel,
    int SymbolCount,
    int SelectedCount,
    int GhostCount,
    string? SelectedSymbolId,
    int EnvelopeRingCount = 0,
    int DatalinkEdgeCount = 0)
{
    public static MapPanelPresentation Empty { get; } = new(string.Empty, 0, 0, 0, null);
}
