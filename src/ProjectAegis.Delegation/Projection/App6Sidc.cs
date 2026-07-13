namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Data-driven APP-6 (2525C) glyph resolver for ADR-007 Phase C.
/// Maps affiliation + lifecycle to unicode fallback glyphs, USS atlas frame ids, and 15-char SIDC strings.
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

    /// <summary>Neutral ground surface unit (Standard Identity N).</summary>
    public const string NeutralUnitSidc = "SNGPU----------";

    /// <summary>Suspect ground surface contact (Standard Identity S).</summary>
    public const string SuspectContactSidc = "SSGPU----------";

    /// <summary>Pending ground surface unit (Standard Identity P).</summary>
    public const string PendingUnitSidc = "SPGPU----------";

    /// <summary>Distinct friendly surface-unit glyph (rectangle frame family).</summary>
    public const string FriendlySurfaceUnitGlyph = "▣";

    /// <summary>Distinct hostile contact glyph (diamond frame family).</summary>
    public const string HostileContactGlyph = "⬥";

    /// <summary>Destroyed friendly unit — hollow frame variant.</summary>
    public const string FriendlyDestroyedGlyph = "▢";

    /// <summary>Neutral unit glyph (square frame family).</summary>
    public const string NeutralUnitGlyph = "◻";

    /// <summary>Suspect contact glyph (diamond frame family, distinct from hostile).</summary>
    public const string SuspectContactGlyph = "◇";

    /// <summary>Pending unit glyph (quatrefoil / circle frame family).</summary>
    public const string PendingUnitGlyph = "◎";

    /// <summary>USS class for friendly rectangle APP-6 frame sprite.</summary>
    public const string FriendlySurfaceUnitFrame = "map-app6-frame--friendly";

    /// <summary>USS class for hostile diamond APP-6 frame sprite.</summary>
    public const string HostileContactFrame = "map-app6-frame--hostile";

    /// <summary>USS class for destroyed friendly hollow frame sprite.</summary>
    public const string FriendlyDestroyedFrame = "map-app6-frame--friendly-destroyed";

    /// <summary>USS class for unknown / fallback frame sprite.</summary>
    public const string FallbackFrame = "map-app6-frame--unknown";

    /// <summary>USS class for neutral square APP-6 frame sprite.</summary>
    public const string NeutralUnitFrame = "map-app6-frame--neutral";

    /// <summary>USS class for suspect diamond APP-6 frame sprite.</summary>
    public const string SuspectContactFrame = "map-app6-frame--suspect";

    /// <summary>USS class for pending quatrefoil APP-6 frame sprite.</summary>
    public const string PendingUnitFrame = "map-app6-frame--pending";

    public static IReadOnlyList<string> KnownUssFrameIds { get; } =
    [
        FriendlySurfaceUnitFrame,
        HostileContactFrame,
        FriendlyDestroyedFrame,
        FallbackFrame,
        NeutralUnitFrame,
        SuspectContactFrame,
        PendingUnitFrame,
    ];

    private static readonly IReadOnlyDictionary<string, App6MapGlyphResolution> AffiliationTable =
        new Dictionary<string, App6MapGlyphResolution>(StringComparer.Ordinal)
        {
            ["Friendly"] = new(FriendlySurfaceUnitGlyph, FriendlySurfaceUnitFrame, FriendlySurfaceUnitSidc),
            ["Hostile"] = new(HostileContactGlyph, HostileContactFrame, HostileContactSidc),
            ["Neutral"] = new(NeutralUnitGlyph, NeutralUnitFrame, NeutralUnitSidc),
            ["Suspect"] = new(SuspectContactGlyph, SuspectContactFrame, SuspectContactSidc),
            ["Pending"] = new(PendingUnitGlyph, PendingUnitFrame, PendingUnitSidc),
        };

    /// <summary>Resolve glyph + SIDC from tactical affiliation and destroyed state.</summary>
    public static (string Glyph, string Sidc) Resolve(string affiliation, bool isDestroyed = false)
    {
        var resolution = ResolveMapGlyph(affiliation, isDestroyed);
        return (resolution.UnicodeGlyph, resolution.Sidc);
    }

    /// <summary>Resolve unicode glyph, USS frame id, and SIDC from affiliation and destroyed state.</summary>
    public static App6MapGlyphResolution ResolveMapGlyph(string affiliation, bool isDestroyed = false)
    {
        if (string.Equals(affiliation, "Friendly", StringComparison.Ordinal) && isDestroyed)
        {
            return new(FriendlyDestroyedGlyph, FriendlyDestroyedFrame, FriendlySurfaceUnitSidc);
        }

        if (AffiliationTable.TryGetValue(affiliation, out var entry))
        {
            return entry;
        }

        return new(FallbackGlyph, FallbackFrame, FallbackSidc);
    }

    /// <summary>Resolve glyph from an optional 15-char SIDC; falls back when missing or malformed.</summary>
    public static string ResolveGlyphFromSidc(string? sidc) =>
        ResolveMapGlyphFromSidc(sidc).UnicodeGlyph;

    /// <summary>Resolve unicode glyph, USS frame id, and SIDC from an optional 15-char SIDC.</summary>
    public static App6MapGlyphResolution ResolveMapGlyphFromSidc(string? sidc)
    {
        if (!TryParseStandardIdentity(sidc, out var identity))
        {
            return new(FallbackGlyph, FallbackFrame, FallbackSidc);
        }

        return identity switch
        {
            'F' or 'A' or 'D' or 'M' or 'J' or 'K' or 'L' => AffiliationTable["Friendly"],
            'H' => AffiliationTable["Hostile"],
            'N' => AffiliationTable["Neutral"],
            'S' => AffiliationTable["Suspect"],
            'P' => AffiliationTable["Pending"],
            _ => new(FallbackGlyph, FallbackFrame, FallbackSidc),
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