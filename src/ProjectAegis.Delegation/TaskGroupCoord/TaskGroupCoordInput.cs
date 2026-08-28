namespace ProjectAegis.Delegation.TaskGroupCoord;

/// <summary>Read-only facts for headless task-group coordination (projection input).</summary>
public sealed record TaskGroupCoordInput(
    string GroupId,
    IReadOnlyList<string> Members,
    string PackageId,
    string PackageLabel,
    bool HasC2,
    string C2NodeId = "",
    bool IsSplit = false);
