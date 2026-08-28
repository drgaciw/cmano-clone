namespace ProjectAegis.Delegation.TaskGroupCoord;

/// <summary>
/// DRG-223: headless task-group / formation coordination posture for Combat UX Slice C.
/// Advisory only — never issues orders, detach, rejoin, replan, or catalog writes.
/// </summary>
public sealed record TaskGroupCoordSnapshot(
    string GroupId,
    IReadOnlyList<string> Members,
    string AssignedPackageId,
    string AssignedPackageLabel,
    string GapCode,
    TaskGroupCoordKind Kind,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    string StatusLine)
{
    /// <summary>Empty sentinel when no group facts are supplied.</summary>
    public static TaskGroupCoordSnapshot Empty { get; } = new(
        GroupId: string.Empty,
        Members: Array.Empty<string>(),
        AssignedPackageId: string.Empty,
        AssignedPackageLabel: string.Empty,
        GapCode: string.Empty,
        Kind: TaskGroupCoordKind.AdvisoryCoordination,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        IsAutomaticEngagement: false,
        StatusLine: "TGC: —");
}
