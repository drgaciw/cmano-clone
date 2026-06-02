namespace ProjectAegis.Delegation.Projection;

public static class MissionListPanelBinder
{
    public static MissionListPanelState Bind(IReadOnlyList<MissionListEntry> entries)
    {
        var rows = new List<MissionListDisplayRow>(entries.Count);
        foreach (var entry in entries)
        {
            rows.Add(new MissionListDisplayRow(
                entry.EventId,
                $"T{entry.FireAtTick}  {entry.EventId}  {entry.Kind}  {entry.Code}"));
        }

        return new MissionListPanelState(rows);
    }
}