namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Data-driven APP-6 (2525C) glyph resolver for ADR-007 Phase C spike.
/// Maps affiliation + lifecycle to distinct Toolkit placeholder glyphs and optional 15-char SIDC strings for a future icon atlas.
/// </summary>
public static class App6Sidc
{
    public const int SidcLength = 15;

    /// <summary>Unknown / invalid SIDC fallback (Standard: Unknown, Land, Protected, Unit).</summary>
    public const string FallbackSidc = "SUZPU----------";

    /// <summary>Neutral placeholder glyph when SIDC is missing or invalid.</summary>
    public const string FallbackGlyph = "●";

    /// <summary>Friendly ground surface unit (Standard Identity F, Battle Dimension G).</summary>
    public const string FriendlySurfaceUnitSidc = "SFGPU----------";

    /// <summary>Hostile ground surface contact.</summary>
    public const string HostileContactSidc = "SHGPU----------";

    /// <summary>Distinct friendly surface-unit glyph (rectangle frame family).</summary>
    public const string FriendlySurfaceUnitGlyph = "▣";

    /// <summary>Distinct hostile contact glyph (diamond frame family).</summary>
    public const string HostileContactGlyph = "⬥";

    /// <summary>Destroyed friendly unit — hollow frame variant.</summary>
    public const string FriendlyDestroyedGlyph = "▢";

    private static readonly IReadOnlyDictionary<string, (string Glyph, string Sidc)> AffiliationTable =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Friendly"] = (FriendlySurfaceUnitGlyph, FriendlySurfaceUnitSidc),
            ["Hostile"] = (HostileContactGlyph, HostileContactSidc),
        };

    /// <summary>Resolve glyph + SIDC from tactical affiliation and destroyed state.</summary>
    public static (string Glyph, string Sidc) Resolve(string affiliation, bool isDestroyed = false)
    {
        if (string.Equals(affiliation, "Friendly", StringComparison.Ordinal) && isDestroyed)
        {
            return (FriendlyDestroyedGlyph, FriendlySurfaceUnitSidc);
        }

        if (AffiliationTable.TryGetValue(affiliation, out var entry))
        {
            return entry;
        }

        return (FallbackGlyph, FallbackSidc);
    }

    /// <summary>Resolve glyph from an optional 15-char SIDC; falls back when missing or malformed.</summary>
    public static string ResolveGlyphFromSidc(string? sidc)
    {
        if (!TryParseStandardIdentity(sidc, out var identity))
        {
            return FallbackGlyph;
        }

        return identity switch
        {
            'F' or 'A' or 'D' or 'M' or 'J' or 'K' or 'L' => FriendlySurfaceUnitGlyph,
            'H' or 'S' => HostileContactGlyph,
            _ => FallbackGlyph,
        };
    }

    /// <summary>Validate a 15-char SIDC string.</summary>
    public static bool IsValidSidc(string? sidc) =>
        !string.IsNullOrWhiteSpace(sidc) && sidc.Length == SidcLength;

    private static bool TryParseStandardIdentity(string? sidc, out char identity)
    {
        identity = default;
        if (!IsValidSidc(sidc))
        {
            return false;
        }

        identity = sidc![1];
        return sidc[0] == 'S';
    }
}