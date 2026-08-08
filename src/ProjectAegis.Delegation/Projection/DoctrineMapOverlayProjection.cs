namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure doctrine map overlay projection (CMD-33).
/// Maps inheritance panel rows onto optional map symbol positions for host counting/bind.
/// </summary>
public static class DoctrineMapOverlayProjection
{
    /// <summary>
    /// Projects doctrine overlay entries from inheritance rows.
    /// When <paramref name="mapSymbols"/> is provided, positions are filled for matching
    /// <see cref="MapSymbolEntry.SymbolId"/> == unit id. Order is deterministic by UnitId.
    /// </summary>
    public static IReadOnlyList<DoctrineMapOverlayEntry> Project(
        IReadOnlyList<DoctrineInheritanceEntry> inheritanceEntries,
        IReadOnlyList<MapSymbolEntry>? mapSymbols = null)
    {
        if (inheritanceEntries is null)
        {
            throw new ArgumentNullException(nameof(inheritanceEntries));
        }

        if (inheritanceEntries.Count == 0)
        {
            return Array.Empty<DoctrineMapOverlayEntry>();
        }

        Dictionary<string, MapSymbolEntry>? byId = null;
        if (mapSymbols is { Count: > 0 })
        {
            byId = new Dictionary<string, MapSymbolEntry>(mapSymbols.Count, StringComparer.Ordinal);
            foreach (var symbol in mapSymbols)
            {
                if (symbol is null || string.IsNullOrWhiteSpace(symbol.SymbolId))
                {
                    continue;
                }

                // First match wins; friendly OOB symbols use UnitId as SymbolId.
                if (!byId.ContainsKey(symbol.SymbolId))
                {
                    byId[symbol.SymbolId] = symbol;
                }
            }
        }

        var results = new List<DoctrineMapOverlayEntry>(inheritanceEntries.Count);
        foreach (var entry in inheritanceEntries
                     .Where(e => e is not null && !string.IsNullOrWhiteSpace(e.UnitId))
                     .OrderBy(e => e.UnitId, StringComparer.Ordinal))
        {
            float? x = null;
            float? y = null;
            if (byId is not null && byId.TryGetValue(entry.UnitId, out var symbol))
            {
                x = symbol.NormalizedX;
                y = symbol.NormalizedY;
            }

            results.Add(new DoctrineMapOverlayEntry(
                entry.UnitId,
                entry.EffectiveRoeLabel ?? string.Empty,
                entry.InheritanceSource ?? string.Empty,
                x,
                y));
        }

        return results;
    }
}
