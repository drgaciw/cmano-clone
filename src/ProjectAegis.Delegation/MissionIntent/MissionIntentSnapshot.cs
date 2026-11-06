namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>
/// DRG-229: headless mission-command intent and constraint posture for Combat UX Slice C.
/// Advisory only — never issues orders, retask enqueue, or catalog writes.
/// </summary>
public sealed record MissionIntentSnapshot(
    string GroupId,
    string UnitId,
    string IntentCode,
    IReadOnlyList<string> Constraints,
    MissionIntentRetaskAdvice AdvisoryRetask,
    MissionIntentKind Kind,
    bool IsOrder,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    string StatusLine)
{
    /// <summary>Empty sentinel when no group or unit facts are supplied.</summary>
    public static MissionIntentSnapshot Empty { get; } = new(
        GroupId: string.Empty,
        UnitId: string.Empty,
        IntentCode: string.Empty,
        Constraints: Array.Empty<string>(),
        AdvisoryRetask: MissionIntentRetaskAdvice.None,
        Kind: MissionIntentKind.AdvisoryIntent,
        IsOrder: false,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        IsAutomaticEngagement: false,
        StatusLine: "MI: —");
}
