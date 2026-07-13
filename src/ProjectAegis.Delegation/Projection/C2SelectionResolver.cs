namespace ProjectAegis.Delegation.Projection;

/// <summary>Resolves default and valid C2 selection ids (presentation layer).</summary>
public static class C2SelectionResolver
{
    public static string? ResolveDefaultFriendlyUnit(IReadOnlyList<OobTreeEntry> oob) =>
        oob
            .Where(u => u.IsAlive)
            .OrderBy(u => u.UnitId, StringComparer.Ordinal)
            .Select(u => u.UnitId)
            .FirstOrDefault()
        ?? oob.OrderBy(u => u.UnitId, StringComparer.Ordinal).Select(u => u.UnitId).FirstOrDefault();

    public static bool TryResolveFriendlyUnitFromSymbol(
        string symbolId,
        IReadOnlyList<MapSymbolEntry> symbols,
        out string unitId)
    {
        var match = symbols.FirstOrDefault(s =>
            s.SymbolId == symbolId && string.Equals(s.Affiliation, "Friendly", StringComparison.Ordinal));
        if (match == null)
        {
            unitId = string.Empty;
            return false;
        }

        unitId = match.SymbolId;
        return true;
    }

    public static bool TryResolveHostileContactFromSymbol(
        string symbolId,
        IReadOnlyList<MapSymbolEntry> symbols,
        out string contactId)
    {
        var match = symbols.FirstOrDefault(s =>
            s.SymbolId == symbolId && string.Equals(s.Affiliation, "Hostile", StringComparison.Ordinal));
        if (match == null)
        {
            contactId = string.Empty;
            return false;
        }

        contactId = match.SymbolId;
        return true;
    }

    /// <summary>
    /// N/P cycle: resolve the next/previous alive friendly unit within the friendly OOB order (req 20
    /// §Keyboard; <c>C2InputActions.CycleUnit</c>; TR-c2-005). Iterates <paramref name="oob"/> in its
    /// given (display) order — not re-sorted — filtering to <see cref="OobTreeEntry.IsAlive"/> units
    /// only, so cycling never lands on a destroyed unit. Wraps around at either end. When
    /// <paramref name="currentUnitId"/> is null/empty or is not present in the alive list, returns the
    /// first (forward) or last (backward) alive unit. Returns null when no unit is alive, or when
    /// <paramref name="oob"/> itself is null/empty (matches the null-tolerant contract of sibling
    /// resolvers in this namespace, e.g. <c>SelectionBoxResolver</c>, <c>CenterOnSelectionResolver</c>).
    /// </summary>
    public static string? CycleUnit(IReadOnlyList<OobTreeEntry> oob, string? currentUnitId, bool forward)
    {
        if (oob == null)
        {
            return null;
        }

        var alive = oob.Where(u => u.IsAlive).Select(u => u.UnitId).ToList();
        if (alive.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(currentUnitId))
        {
            return forward ? alive[0] : alive[^1];
        }

        var index = alive.IndexOf(currentUnitId);
        if (index < 0)
        {
            return forward ? alive[0] : alive[^1];
        }

        var nextIndex = forward
            ? (index + 1) % alive.Count
            : (index - 1 + alive.Count) % alive.Count;
        return alive[nextIndex];
    }
}