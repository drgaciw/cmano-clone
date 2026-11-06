namespace ProjectAegis.Delegation.ResourceRank;

/// <summary>Per-dimension scarcity scores used to rank eligible candidates (0–1 each).</summary>
public sealed record ResourceRankScores(
    double ExpectedEffect,
    double Time,
    double Availability,
    double Commitment,
    double Conservation,
    double Total)
{
    public static ResourceRankScores Zero { get; } = new(0, 0, 0, 0, 0, 0);
}
