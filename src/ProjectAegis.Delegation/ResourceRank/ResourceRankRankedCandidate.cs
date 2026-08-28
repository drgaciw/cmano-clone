namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>One ranked, alternative, or excluded shooter/weapon under scarcity.</summary>
public sealed record ResourceRankRankedCandidate(
    string ContactId,
    string TargetId,
    string ShooterUnitId,
    string WeaponId,
    string WeaponLabel,
    ResourceRankPosture Posture,
    ResourceRankDisposition Disposition,
    int Rank,
    ResourceRankScores Scores,
    string? ReasonCode,
    string ReasonPlain,
    string StatusLine)
{
    /// <summary>Empty sentinel for missing candidates.</summary>
    public static ResourceRankRankedCandidate Empty { get; } = new(
        ContactId: string.Empty,
        TargetId: string.Empty,
        ShooterUnitId: string.Empty,
        WeaponId: string.Empty,
        WeaponLabel: string.Empty,
        Posture: ResourceRankPosture.Offensive,
        Disposition: ResourceRankDisposition.Excluded,
        Rank: 0,
        Scores: ResourceRankScores.Zero,
        ReasonCode: null,
        ReasonPlain: "No candidate supplied.",
        StatusLine: "RANK: —");
}
