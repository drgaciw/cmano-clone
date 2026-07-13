namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps <see cref="OobTreeEntry"/> list to UI Toolkit ListView rows.</summary>
public static class OobTreePanelBinder
{
    public static OobTreePanelState Bind(IReadOnlyList<OobTreeEntry> entries) =>
        Bind(entries, selectedUnitId: null);

    public static OobTreePanelState Bind(IReadOnlyList<OobTreeEntry> entries, string? selectedUnitId)
    {
        var rows = new List<OobTreeDisplayRow>(entries.Count);
        foreach (var entry in entries)
        {
            var status = entry.IsAlive ? "ALIVE" : "DESTROYED";
            var selected = !string.IsNullOrEmpty(selectedUnitId) && entry.UnitId == selectedUnitId;
            var style = selected ? "oob-row--selected" : "oob-row";
            rows.Add(new OobTreeDisplayRow(
                entry.UnitId,
                $"{entry.UnitId}  [{status}]",
                entry.IsAlive,
                selected,
                style));
        }

        return new OobTreePanelState(rows);
    }

    // S37-04: graph surfacing extension — mark rows in highlight chain (for C2 panel viewer)
    public static OobTreePanelState Bind(IReadOnlyList<OobTreeEntry> entries, string? selectedUnitId, IReadOnlyList<string>? graphHighlightIds)
    {
        var highlights = graphHighlightIds ?? Array.Empty<string>();
        var rows = new List<OobTreeDisplayRow>(entries.Count);
        foreach (var entry in entries)
        {
            var status = entry.IsAlive ? "ALIVE" : "DESTROYED";
            var selected = !string.IsNullOrEmpty(selectedUnitId) && entry.UnitId == selectedUnitId;
            var inGraph = highlights.Contains(entry.UnitId);
            var style = selected ? "oob-row--selected" : (inGraph ? "oob-row--graph" : "oob-row");
            var label = inGraph && !selected ? $"{entry.UnitId}  [{status}] ⚡" : $"{entry.UnitId}  [{status}]";
            rows.Add(new OobTreeDisplayRow(
                entry.UnitId,
                label,
                entry.IsAlive,
                selected,
                style));
        }

        return new OobTreePanelState(rows);
    }
}