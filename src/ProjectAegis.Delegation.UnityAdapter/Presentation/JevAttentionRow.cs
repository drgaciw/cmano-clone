namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One advisory attention row. No order, weapon, or enqueue fields.</summary>
public sealed record JevAttentionRow(
    string UnitId,
    string? ContactId,
    string? Level,
    int Rank,
    bool Highlight,
    string CssClass,
    string Line);
