namespace ProjectAegis.Delegation.Projection;

/// <summary>Resolves default and valid C2 selection ids (presentation layer).</summary>
public static class C2SelectionResolver
{
    /// <summary>
    /// Default startup selection: first alive player-side unit by ordinal id.
    /// Legacy rows fall back to Baltic side affiliation.
    /// </summary>
    public static string? ResolveDefaultFriendlyUnit(IReadOnlyList<OobTreeEntry> oob)
    {
        var candidates = oob
            .Where(u => u.IsPlayerCommandTarget
                ?? !ProjectAegis.Sim.Scenario.BalticV3SideRegistry.IsRedForceUnit(u.UnitId))
            .OrderBy(u => u.UnitId, StringComparer.Ordinal)
            .ToList();
        return candidates.Where(u => u.IsAlive).Select(u => u.UnitId).FirstOrDefault()
            ?? candidates.Select(u => u.UnitId).FirstOrDefault();
    }

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
}