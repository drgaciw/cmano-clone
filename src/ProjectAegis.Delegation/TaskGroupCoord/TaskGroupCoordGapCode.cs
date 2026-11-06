namespace ProjectAegis.Delegation.TaskGroupCoord;

/// <summary>Named coordination gaps for task-group formation posture (never silent drops).</summary>
public static class TaskGroupCoordGapCode
{
    /// <summary>Group has members, assigned package, C2 present, and is not split.</summary>
    public const string None = "NONE";

    /// <summary>No reachable C2 / command node for the group.</summary>
    public const string NoC2 = "NO_C2";

    /// <summary>Formation is split or members are detached from the group.</summary>
    public const string Split = "SPLIT";

    /// <summary>No mission package is assigned to the group.</summary>
    public const string Unassigned = "UNASSIGNED";
}
