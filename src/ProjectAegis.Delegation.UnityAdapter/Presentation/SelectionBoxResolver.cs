using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Normalized (0..1) marquee rectangle in map-canvas space (req 20 §Selection, TR-c2-005). Corner
/// order is not significant — <see cref="FromCorners"/> normalizes min/max on construction.
/// </summary>
public readonly record struct NormalizedRect(float MinX, float MinY, float MaxX, float MaxY)
{
    /// <summary>Build a rect from two arbitrary (pointer-down, pointer-up) corners.</summary>
    public static NormalizedRect FromCorners(float x0, float y0, float x1, float y1) =>
        new(Math.Min(x0, x1), Math.Min(y0, y1), Math.Max(x0, x1), Math.Max(y0, y1));

    /// <summary>True if the point lies within the rect (inclusive bounds).</summary>
    public bool Contains(float x, float y) => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

    /// <summary>
    /// Width/height below this (normalized 0..1) threshold are treated as a click, not a drag — the
    /// host uses this to distinguish a marquee gesture from a plain single-select click.
    /// </summary>
    public const float DragThreshold = 0.01f;

    /// <summary>True when the rect is large enough to be a deliberate drag-box rather than a click.</summary>
    public bool IsDrag => (MaxX - MinX) > DragThreshold || (MaxY - MinY) > DragThreshold;
}

/// <summary>
/// Pure rect→unit-ids resolver for map drag-box multi-select (req 20 §Selection, TR-c2-005). Never
/// touches UnityEngine types — the host (<c>MapPlaceholderPanelHost</c>) only feeds pointer-derived
/// normalized coordinates in and applies the resulting ordered id list to
/// <see cref="C2PresentationController"/>.
/// </summary>
public static class SelectionBoxResolver
{
    /// <summary>
    /// Resolve every live friendly unit symbol whose normalized position falls inside
    /// <paramref name="rect"/>, in <paramref name="symbols"/> input order (deterministic; mirrors
    /// map-picture build order). Destroyed and non-friendly symbols are never selectable via marquee.
    /// </summary>
    public static IReadOnlyList<string> Resolve(NormalizedRect rect, IReadOnlyList<MapSymbolEntry> symbols)
    {
        if (symbols == null || symbols.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new List<string>();
        foreach (var symbol in symbols)
        {
            if (symbol.IsDestroyed)
            {
                continue;
            }

            if (!string.Equals(symbol.Affiliation, "Friendly", StringComparison.Ordinal))
            {
                continue;
            }

            if (!rect.Contains(symbol.NormalizedX, symbol.NormalizedY))
            {
                continue;
            }

            result.Add(symbol.SymbolId);
        }

        return result;
    }
}
