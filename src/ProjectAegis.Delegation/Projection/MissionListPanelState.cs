namespace ProjectAegis.Delegation.Projection;

public sealed record MissionListPanelState(IReadOnlyList<MissionListDisplayRow> MissionRows);

public sealed record MissionListDisplayRow(string EventId, string DisplayLine);