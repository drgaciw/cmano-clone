namespace ProjectAegis.Delegation.Projection;

/// <summary>Maps <see cref="OobTreeEntry"/> list to UI Toolkit ListView rows.</summary>
public static class OobTreePanelBinder
{
    public static OobTreePanelState Bind(IReadOnlyList<OobTreeEntry> entries)
    {
        var rows = new List<OobTreeDisplayRow>(entries.Count);
        foreach (var entry in entries)
        {
            var status = entry.IsAlive ? "ALIVE" : "DESTROYED";
            rows.Add(new OobTreeDisplayRow(
                entry.UnitId,
                $"{entry.UnitId}  [{status}]",
                entry.IsAlive));
        }

        return new OobTreePanelState(rows);
    }
}