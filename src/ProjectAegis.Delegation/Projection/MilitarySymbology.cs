namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SYM-MIL-01 Military Tactical profile resolver (DRG-231, S126 MIL-only thin slice).
/// Declared subset of NATO APP-6(C) / MIL-STD-2525C 15-character SIDCs plus an NTDS
/// surface/subsurface frame convention. Not a certified implementation of either standard.
/// </summary>
public static class MilitarySymbology
{
    /// <summary>Pinned APP-6 edition for this subset.</summary>
    public const string App6Edition = "NATO APP-6(C)";

    /// <summary>Pinned MIL-STD-2525 edition for this subset (15-character letter SIDC).</summary>
    public const string MilStd2525Edition = "MIL-STD-2525C";

    /// <summary>Supported NTDS subset.</summary>
    public const string NtdsSubset =
        "NTDS track frames: surface = full frame, subsurface = lower-half frame; " +
        "no NTDS air, ESM, or SSDS modifiers";

    /// <summary>Frozen standard identities (W2-SYM-05 affiliation expansion HOLD).</summary>
    public static IReadOnlyList<string> SupportedAffiliations { get; } =
        ["Friendly", "Hostile", "Neutral", "Unknown"];

    /// <summary>Documented deviations from the pinned editions.</summary>
    public static IReadOnlyList<string> Deviations { get; } =
    [
        "Proposed MIL-STD-2525E Change 1 (20/30-digit SIDC) baseline not adopted in this slice; 2525C letter SIDCs used.",
        "Suspect, Pending, Assumed Friend, Joker and Faker identities render as Unknown (W2-SYM-05 HOLD).",
        "Unicode frame glyphs and text icons stand in for licensed artwork; no CMANO/CMO artwork.",
        "Platform class is never shown; scoped categories stop at combatant / submarine / unknown surface.",
    ];

    private static readonly IReadOnlyDictionary<string, char> IdentityCodes =
        new Dictionary<string, char>(StringComparer.Ordinal)
        {
            ["Friendly"] = 'F',
            ["Hostile"] = 'H',
            ["Neutral"] = 'N',
            ["Unknown"] = 'U',
        };

    private static readonly IReadOnlyDictionary<string, string> FullFrameGlyphs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Friendly"] = "▣",
            ["Hostile"] = "◆",
            ["Neutral"] = "■",
            ["Unknown"] = "✤",
        };

    private static readonly IReadOnlyDictionary<string, string> LowerHalfFrameGlyphs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Friendly"] = "◡",
            ["Hostile"] = "▽",
            ["Neutral"] = "⊔",
            ["Unknown"] = "⌣",
        };

    /// <summary>Maps any affiliation outside the frozen set (including null) to <c>Unknown</c>.</summary>
    public static string NormalizeAffiliation(string? affiliation) =>
        affiliation is not null && IdentityCodes.ContainsKey(affiliation) ? affiliation : "Unknown";

    /// <summary>
    /// Resolves the MIL glyph for an observed affiliation and observed symbol key.
    /// Unknown keys and affiliations fall back observer-safely; destroyed status is only for own units.
    /// </summary>
    public static SymbologyGlyph Resolve(string? affiliation, string? observedSymbolKey, bool isDestroyed = false)
    {
        var normalized = NormalizeAffiliation(affiliation);
        var definition = SymbolKeyRegistry.ResolveObserved(observedSymbolKey);
        var status = isDestroyed ? 'X' : 'P';
        var sidc = $"S{IdentityCodes[normalized]}{definition.BattleDimension}{status}{definition.FunctionId}-----";
        var lowerHalf = definition.BattleDimension == 'U';
        var glyph = lowerHalf ? LowerHalfFrameGlyphs[normalized] : FullFrameGlyphs[normalized];
        var frame = $"map-sym-mil--{normalized.ToLowerInvariant()}-{DimensionName(definition.BattleDimension)}";
        if (isDestroyed)
        {
            frame += "--destroyed";
        }

        return new SymbologyGlyph(definition.Key, normalized, glyph, frame, sidc, definition.IconText);
    }

    private static string DimensionName(char battleDimension) => battleDimension switch
    {
        'S' => "surface",
        'U' => "subsurface",
        _ => "unknown",
    };
}
