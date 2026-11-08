using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>Associates known contact map poses with combat target identities; never queries world truth.</summary>
public static class CombatMapSymbolBridge
{
    /// <summary>
    /// Adds target aliases using the freshest displayed contact, breaking ties by contact id.
    /// The original map symbols are unchanged. Unmapped targets remain absent, not fabricated.
    /// </summary>
    public static IReadOnlyList<MapSymbolEntry> Build(
        IReadOnlyList<MapSymbolEntry> symbols, IReadOnlyList<ContactPictureEntry> contacts)
    {
        var byId = new Dictionary<string, MapSymbolEntry>(StringComparer.Ordinal);
        foreach (var symbol in symbols) byId.TryAdd(symbol.SymbolId, symbol);
        var result = new List<MapSymbolEntry>(symbols);
        var assigned = new HashSet<string>(byId.Keys, StringComparer.Ordinal);
        foreach (var contact in contacts.OrderByDescending(c => c.LastSimTime).ThenBy(c => c.ContactId, StringComparer.Ordinal))
        {
            if (byId.TryGetValue(contact.ContactId, out var pose) && assigned.Add(contact.TargetId))
                result.Add(pose with { SymbolId = contact.TargetId });
        }
        return Array.AsReadOnly(result.ToArray());
    }
}
