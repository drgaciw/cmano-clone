namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless apply path for basemap layer checklist presentation (CMD-28.2).
/// Hosts bind lines onto Labels / Toggle rows without re-formatting.
/// </summary>
public static class MapLayerStackApplyState
{
    public static MapLayerStackPresentation Apply(MapLayerStackState? state)
    {
        if (state is null || state.Count == 0)
        {
            return MapLayerStackPresentation.Empty;
        }

        var lines = new List<MapLayerChecklistLine>(state.Count);
        var displayLines = new List<string>(state.Count);
        foreach (var layer in state.Layers)
        {
            if (layer is null)
            {
                continue;
            }

            var mark = layer.IsVisible ? "[x]" : "[ ]";
            var shortcut = string.IsNullOrWhiteSpace(layer.ShortcutHint)
                ? "none"
                : layer.ShortcutHint;
            var display = $"{mark} {layer.Label}  ({shortcut})";
            lines.Add(new MapLayerChecklistLine(
                Id: layer.Id.ToString(),
                Label: layer.Label,
                IsVisible: layer.IsVisible,
                ShortcutHint: shortcut,
                DisplayLine: display));
            displayLines.Add(display);
        }

        var visible = state.VisibleCount;
        var total = lines.Count;
        return new MapLayerStackPresentation(
            Lines: lines,
            DisplayLines: displayLines,
            VisibleCount: visible,
            TotalCount: total,
            SummaryLabel: $"LAYERS: {visible}/{total}");
    }

    /// <summary>Project defaults then apply — headless product path for checklist smoke tests.</summary>
    public static MapLayerStackPresentation ProjectAndApplyDefaults() =>
        Apply(MapLayerStackProjection.DefaultStack());
}

/// <summary>One checklist row for basemap layer UI.</summary>
public sealed record MapLayerChecklistLine(
    string Id,
    string Label,
    bool IsVisible,
    string ShortcutHint,
    string DisplayLine);

/// <summary>Presentation bundle for map layer stack hosts / tests.</summary>
public sealed record MapLayerStackPresentation(
    IReadOnlyList<MapLayerChecklistLine> Lines,
    IReadOnlyList<string> DisplayLines,
    int VisibleCount,
    int TotalCount,
    string SummaryLabel)
{
    public static MapLayerStackPresentation Empty { get; } = new(
        Array.Empty<MapLayerChecklistLine>(),
        Array.Empty<string>(),
        VisibleCount: 0,
        TotalCount: 0,
        SummaryLabel: "LAYERS: 0/0");
}
