namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One advisory attention row. No order, weapon, or enqueue fields.</summary>
public sealed record JevAttentionRow(
    string UnitId,
    string? ContactId,
    string? Level,
    int Rank,
    bool Highlight,
    string CssClass)
{
    /// <summary>Advisory line. The contact id stays on the row so the text cannot drift from it.</summary>
    public string Line
    {
        get
        {
            var shown = Level ?? JevAttentionLevel.Unavailable;
            var contact = string.IsNullOrEmpty(ContactId) ? string.Empty : $"  {ContactId}";
            return $"ATTN {shown}  {UnitId}{contact}";
        }
    }
}
