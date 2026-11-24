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

    /// <summary>
    /// Resolves a clicked hostile map symbol to a contact id (UAT D36). The symbol may be the contact
    /// marker itself (id equals a contact id) or the hostile unit's own marker (id equals a contact's
    /// <c>TargetId</c>); the latter maps to the tracking contact. Returns false when no contact exists,
    /// so no empty Contact Detail is opened.
    /// </summary>
    public static bool TryResolveHostileContactFromSymbol(
        string symbolId,
        IReadOnlyList<MapSymbolEntry> symbols,
        IReadOnlyList<ContactPictureEntry> contacts,
        out string contactId)
    {
        contactId = string.Empty;
        if (!symbols.Any(s =>
                s.SymbolId == symbolId && string.Equals(s.Affiliation, "Hostile", StringComparison.Ordinal)))
        {
            return false;
        }

        if (contacts.Any(c => string.Equals(c.ContactId, symbolId, StringComparison.Ordinal)))
        {
            contactId = symbolId;
            return true;
        }

        var tracking = contacts
            .Where(c => string.Equals(c.TargetId, symbolId, StringComparison.Ordinal))
            .OrderBy(c => c.ContactId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (tracking == null)
        {
            return false;
        }

        contactId = tracking.ContactId;
        return true;
    }
}
