namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Per-unit doctrine ROE/source row for map overlay (CMD-33).
/// Optional normalized positions come from matching <see cref="MapSymbolEntry"/> rows.
/// </summary>
public sealed record DoctrineMapOverlayEntry(
    string UnitId,
    string RoeLabel,
    string SourceLabel,
    float? NormalizedX = null,
    float? NormalizedY = null);
