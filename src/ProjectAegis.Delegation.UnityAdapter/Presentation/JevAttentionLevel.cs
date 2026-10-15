namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-319 attention Score bands for <c>attention_priority</c>. Presentation chrome only.
/// </summary>
public static class JevAttentionLevel
{
    /// <summary>No highlight; bottom of the attention sort.</summary>
    public const string Ignore = "ignore";

    /// <summary>Soft optional glance.</summary>
    public const string Low = "low";

    /// <summary>Secondary panel / soft highlight.</summary>
    public const string Watch = "watch";

    /// <summary>Primary panel / strong highlight.</summary>
    public const string Focus = "focus";

    /// <summary>Interrupt chrome; top of the attention sort.</summary>
    public const string Immediate = "immediate";

    /// <summary>Fail-soft band when Jev decide is offline. Not a model score.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>Higher ranks sort first.</summary>
    public static int Rank(string? level) => level switch
    {
        Immediate => 5,
        Focus => 4,
        Watch => 3,
        Low => 2,
        Ignore => 1,
        _ => 0,
    };

    /// <summary>Ignore and unavailable rows are listed without a highlight.</summary>
    public static bool Highlights(string? level) =>
        level is Immediate or Focus or Watch or Low;

    /// <summary>USS class for one band. Ignore stays unhighlighted.</summary>
    public static string CssClass(string? level) => level switch
    {
        Immediate => "jev-attention-row--immediate",
        Focus => "jev-attention-row--focus",
        Watch => "jev-attention-row--watch",
        Low => "jev-attention-row--low",
        Ignore => "jev-attention-row--ignore",
        _ => "jev-attention-row--unavailable",
    };
}
