namespace ProjectAegis.Ai.Contracts;

/// <summary>Side filter applied before prompt construction. Debrief mode may expose full truth.</summary>
public enum ViewingSide
{
    Unknown = 0,
    Blue = 1,
    Red = 2,
    Neutral = 3,
    /// <summary>Explicit post-run debrief; not the default player view.</summary>
    Debrief = 4,
}
