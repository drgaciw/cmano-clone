namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Declutter priority tiers per req 20 rev 2 §Map and Symbology:
/// "declutter by priority (selected &gt; engaged &gt; hostile &gt; friendly) with leader lines before hiding".
/// Lower numeric value == higher priority == placed first.
/// </summary>
public enum MapLabelPriority
{
    Selected = 0,
    Engaged = 1,
    Hostile = 2,
    Friendly = 3,
    Other = 4,
}

/// <summary>Outcome of label declutter for a single map symbol's text label (icon/frame is always shown).</summary>
public enum MapLabelDeclutterOutcome
{
    /// <summary>Label rendered directly at the symbol's position.</summary>
    Shown,

    /// <summary>Label rendered offset from the symbol with a connecting leader line (used before hiding).</summary>
    LeaderLine,

    /// <summary>Label suppressed entirely; only the icon/frame remains visible.</summary>
    Hidden,
}

/// <summary>Input candidate for label declutter — one per map symbol whose label may need to be placed.</summary>
public sealed record MapLabelCandidate(
    string SymbolId,
    MapLabelPriority Priority,
    float NormalizedX,
    float NormalizedY);

/// <summary>Declutter result for a single symbol's label.</summary>
public sealed record MapLabelDeclutterResult(string SymbolId, MapLabelDeclutterOutcome Outcome);

/// <summary>
/// Pure ordering/assignment function for map label declutter (req 20 rev 2 §Map and Symbology).
/// Given labels + priorities + a collision radius, decides which labels show directly, which get a
/// leader line, and which are hidden — leader lines are always exhausted before any label is hidden,
/// and higher-priority labels always win contested slots (selected &gt; engaged &gt; hostile &gt; friendly &gt; other).
/// No UnityEngine / rendering dependency; deterministic for a given input (stable tie-break by SymbolId).
/// </summary>
public static class MapLabelDeclutterProjection
{
    /// <summary>
    /// Resolve declutter outcomes for all candidates.
    /// </summary>
    /// <param name="candidates">Label candidates (order-independent; result preserves input order).</param>
    /// <param name="collisionRadius">Normalized-space radius under which two labels are considered colliding.</param>
    /// <param name="maxDirectLabels">Max labels shown at full position before falling back to leader lines.</param>
    /// <param name="maxLeaderLineLabels">Max additional labels shown via leader line before the rest are hidden.</param>
    public static IReadOnlyList<MapLabelDeclutterResult> Resolve(
        IReadOnlyList<MapLabelCandidate> candidates,
        float collisionRadius,
        int maxDirectLabels,
        int maxLeaderLineLabels)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var ordered = candidates
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.SymbolId, StringComparer.Ordinal)
            .ToList();

        var placedDirect = new List<MapLabelCandidate>();
        var directCount = 0;
        var leaderCount = 0;
        var outcomeById = new Dictionary<string, MapLabelDeclutterOutcome>(candidates.Count, StringComparer.Ordinal);

        foreach (var candidate in ordered)
        {
            var collides = CollidesWithAny(candidate, placedDirect, collisionRadius);

            if (!collides && directCount < maxDirectLabels)
            {
                outcomeById[candidate.SymbolId] = MapLabelDeclutterOutcome.Shown;
                placedDirect.Add(candidate);
                directCount++;
            }
            else if (leaderCount < maxLeaderLineLabels)
            {
                outcomeById[candidate.SymbolId] = MapLabelDeclutterOutcome.LeaderLine;
                leaderCount++;
            }
            else
            {
                outcomeById[candidate.SymbolId] = MapLabelDeclutterOutcome.Hidden;
            }
        }

        var results = new List<MapLabelDeclutterResult>(candidates.Count);
        foreach (var candidate in candidates)
        {
            results.Add(new MapLabelDeclutterResult(candidate.SymbolId, outcomeById[candidate.SymbolId]));
        }

        return results;
    }

    private static bool CollidesWithAny(
        MapLabelCandidate candidate,
        IReadOnlyList<MapLabelCandidate> placed,
        float collisionRadius)
    {
        foreach (var other in placed)
        {
            var dx = candidate.NormalizedX - other.NormalizedX;
            var dy = candidate.NormalizedY - other.NormalizedY;
            if (Math.Sqrt((dx * dx) + (dy * dy)) < collisionRadius)
            {
                return true;
            }
        }

        return false;
    }
}
