namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Canonical symbol key registry (S126-03 W2-SYM-01, DRG-231). Keys are presentation-only,
/// ordinal-stable strings; they are never fed into sim state, order logs or replay hashes.
/// </summary>
public static class SymbolKeyRegistry
{
    /// <summary>Registry schema version; bump when a key string changes meaning.</summary>
    public const string SchemaVersion = "aegis-sym-keys/v1";

    /// <summary>Generic unknown — observer-safe fallback when type or domain is not known.</summary>
    public const string GenericUnknown = "generic.unknown";

    /// <summary>Naval surface combatant (class not disclosed).</summary>
    public const string NavalSurfaceCombatant = "naval.surface.combatant";

    /// <summary>Naval subsurface submarine (class not disclosed).</summary>
    public const string NavalSubsurfaceSubmarine = "naval.subsurface.submarine";

    /// <summary>Naval surface vessel of unknown category.</summary>
    public const string NavalSurfaceUnknown = "naval.surface.unknown";

    private const string Std2525CSeaSurface = "MIL-STD-2525C Appendix A, battle dimension S (sea surface)";
    private const string Std2525CSubsurface = "MIL-STD-2525C Appendix A, battle dimension U (subsurface)";
    private const string Std2525CUnknownDimension = "MIL-STD-2525C Appendix A, battle dimension Z (unknown)";

    private static readonly IReadOnlyDictionary<string, SymbolKeyDefinition> ByKey =
        new Dictionary<string, SymbolKeyDefinition>(StringComparer.Ordinal)
        {
            [GenericUnknown] = new(GenericUnknown, "generic", "unknown", 'Z', "------", "?", GenericUnknown, Std2525CUnknownDimension),
            [NavalSurfaceCombatant] = new(NavalSurfaceCombatant, "naval", "surface-combatant", 'S', "C-----", "CBT", NavalSurfaceUnknown, Std2525CSeaSurface),
            [NavalSubsurfaceSubmarine] = new(NavalSubsurfaceSubmarine, "naval", "submarine", 'U', "S-----", "SUB", GenericUnknown, Std2525CSubsurface),
            [NavalSurfaceUnknown] = new(NavalSurfaceUnknown, "naval", "surface-unknown", 'S', "------", "?", GenericUnknown, Std2525CSeaSurface),
        };

    /// <summary>All entries, ordinal-sorted by key.</summary>
    public static IReadOnlyList<SymbolKeyDefinition> All { get; } =
        Array.AsReadOnly(ByKey.Values.OrderBy(d => d.Key, StringComparer.Ordinal).ToArray());

    /// <summary>S126-05 scoped naval types (MIL profile thin slice).</summary>
    public static IReadOnlyList<string> ScopedNavalKeys { get; } =
    [
        NavalSurfaceCombatant,
        NavalSubsurfaceSubmarine,
        NavalSurfaceUnknown,
    ];

    /// <summary>Exact ordinal lookup.</summary>
    public static bool TryGet(string? key, out SymbolKeyDefinition definition)
    {
        if (key is not null && ByKey.TryGetValue(key, out var found))
        {
            definition = found;
            return true;
        }

        definition = ByKey[GenericUnknown];
        return false;
    }

    /// <summary>
    /// Resolves an observer-supplied key; missing or unrecognised keys yield <see cref="GenericUnknown"/>
    /// so the display never invents a category the observer does not hold.
    /// </summary>
    public static SymbolKeyDefinition ResolveObserved(string? observedKey)
    {
        TryGet(observedKey, out var definition);
        return definition;
    }

    /// <summary>Lowercase ASCII segments separated by dots; segments may contain hyphens.</summary>
    public static bool IsCanonicalKeyFormat(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var segments = key.Split('.');
        return segments.Length >= 2 && segments.All(IsCanonicalSegment);
    }

    private static bool IsCanonicalSegment(string segment) =>
        segment.Length > 0
        && segment[0] != '-'
        && segment[^1] != '-'
        && segment.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');
}
