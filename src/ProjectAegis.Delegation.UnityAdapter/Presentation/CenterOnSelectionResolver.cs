using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Normalized (0..1) map-space point the camera/view should center on.</summary>
public readonly record struct CenterOnSelectionTarget(float NormalizedX, float NormalizedY);

/// <summary>
/// Pure centroid resolver for "center on selection" (req 20 §Selection, TR-c2-005; F key /
/// <see cref="ProjectAegis.Delegation.Input.C2InputActions.FocusPrimaryThreat"/> shares the same
/// centering primitive for a multi-unit friendly selection). Centers on the mean position of every
/// currently selected unit that has a live (non-destroyed) friendly symbol on the map; units whose
/// symbol is missing or destroyed are excluded from the average rather than failing the whole
/// resolve. Returns null only when the selection is empty or none of it currently resolves to a
/// live map symbol.
/// </summary>
public static class CenterOnSelectionResolver
{
    public static CenterOnSelectionTarget? Resolve(SelectionSet selection, IReadOnlyList<MapSymbolEntry> symbols)
    {
        if (selection == null || selection.IsEmpty || symbols == null || symbols.Count == 0)
        {
            return null;
        }

        var sumX = 0f;
        var sumY = 0f;
        var count = 0;

        foreach (var unitId in selection.OrderedTargetIds)
        {
            var symbol = FindLiveFriendlySymbol(symbols, unitId);
            if (symbol == null)
            {
                continue;
            }

            sumX += symbol.NormalizedX;
            sumY += symbol.NormalizedY;
            count++;
        }

        return count == 0 ? null : new CenterOnSelectionTarget(sumX / count, sumY / count);
    }

    private static MapSymbolEntry? FindLiveFriendlySymbol(IReadOnlyList<MapSymbolEntry> symbols, string unitId)
    {
        foreach (var symbol in symbols)
        {
            if (symbol.IsDestroyed)
            {
                continue;
            }

            if (string.Equals(symbol.SymbolId, unitId, StringComparison.Ordinal) &&
                string.Equals(symbol.Affiliation, "Friendly", StringComparison.Ordinal))
            {
                return symbol;
            }
        }

        return null;
    }
}
