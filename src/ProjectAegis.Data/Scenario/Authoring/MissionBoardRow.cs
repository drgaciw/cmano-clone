namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>One row on the Mission Board list (AME-3.4).</summary>
public sealed class MissionBoardRow
{
    /// <summary>Mission id.</summary>
    public string Id { get; init; } = "";

    /// <summary>Mission type: Patrol|Strike|Ferry|Support.</summary>
    public string Type { get; init; } = "";

    /// <summary>Side derived from the first assigned ORBAT unit; null when unassigned or side unknown.</summary>
    public string? SideId { get; init; }

    /// <summary><c>Assigned</c> when unit count &gt; 0; otherwise <c>Unassigned</c>.</summary>
    public string Status { get; init; } = "Unassigned";

    /// <summary>Count of assigned unit ids.</summary>
    public int UnitCount { get; init; }

    /// <summary>Short display line for list UIs.</summary>
    public string SummaryLine { get; init; } = "";
}
