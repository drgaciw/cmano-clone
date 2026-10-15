namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One advisory attention row. No order, weapon, or enqueue fields.</summary>
public sealed record JevAttentionRow(
    string UnitId,
    string? ContactId,
    string? Level,
    int Rank,
    bool Highlight,
    string CssClass,
    string Line)
{
    /// <summary>Selected or camera-focused rows follow unattended rows of equal priority.</summary>
    public bool PlayerAlreadyLooking { get; init; }
}
