namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>Deterministic tactical map symbols until sim publishes world coordinates.</summary>
public static class MapPictureProjection
{
    public static IReadOnlyList<MapSymbolEntry> Project(
        IReadOnlyList<OobTreeEntry> oob,
        IReadOnlyList<ContactPictureEntry> contacts,
        int layoutSeed)
    {
        var symbols = new List<MapSymbolEntry>(oob.Count + contacts.Count);
        foreach (var unit in oob)
        {
            var (x, y) = Place(unit.UnitId, layoutSeed);
            symbols.Add(new MapSymbolEntry(
                unit.UnitId,
                "Friendly",
                "■",
                unit.UnitId,
                x,
                y,
                !unit.IsAlive));
        }

        foreach (var contact in contacts)
        {
            var (x, y) = Place(contact.ContactId, layoutSeed + 17);
            symbols.Add(new MapSymbolEntry(
                contact.ContactId,
                "Hostile",
                "◆",
                $"{contact.ContactId} {contact.LifecycleState}",
                x,
                y,
                IsDestroyed: false));
        }

        return symbols
            .OrderBy(s => s.Affiliation, StringComparer.Ordinal)
            .ThenBy(s => s.SymbolId, StringComparer.Ordinal)
            .ToArray();
    }

    public static (float X, float Y) Place(string key, int seed)
    {
        var h = DeterministicHash.OrdinalHash($"{seed}:{key}");
        var hx = (uint)(h & 0xFFFF);
        var hy = (uint)((h >> 16) & 0xFFFF);
        var x = hx / 65535f * 0.75f + 0.1f;
        var y = hy / 65535f * 0.75f + 0.1f;
        return (x, y);
    }
}