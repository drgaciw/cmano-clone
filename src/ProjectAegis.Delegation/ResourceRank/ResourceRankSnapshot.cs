namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>
/// DRG-217: headless ranked shooter/weapon list under scarcity for Combat UX Slice C.
/// Advisory only — never authorizes weapons release, never enqueues fire, never auto-engages.
/// </summary>
public sealed record ResourceRankSnapshot(
    string ContactId,
    string TargetId,
    ResourceRankKind Kind,
    bool IsWeaponsReleaseAuthorization,
    bool IsFireOrder,
    bool IsAutomaticEngagement,
    IReadOnlyList<ResourceRankRankedCandidate> RankedCandidates,
    string StatusLine)
{
    /// <summary>Empty sentinel when no candidates are supplied.</summary>
    public static ResourceRankSnapshot Empty { get; } = new(
        ContactId: string.Empty,
        TargetId: string.Empty,
        Kind: ResourceRankKind.AdvisoryRanking,
        IsWeaponsReleaseAuthorization: false,
        IsFireOrder: false,
        IsAutomaticEngagement: false,
        RankedCandidates: Array.Empty<ResourceRankRankedCandidate>(),
        StatusLine: "RANK: —");
}
