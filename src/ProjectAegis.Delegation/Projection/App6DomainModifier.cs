namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;

/// <summary>
/// APP-6(D) battle-dimension (domain) modifier for the map placeholder (req 20 rev 2 §Map and Symbology).
/// Domain modifiers are a SECONDARY cue layered on top of the affiliation frame — affiliation shape
/// (<see cref="App6Sidc"/> frame ids) remains the primary, grayscale-safe cue per
/// <c>design/accessibility-requirements.md</c> §5. Covers the domains referenced by docs 13–17
/// (<see cref="CombatDomain"/>): Air, Surface, Subsurface, Land, Mine, Facility.
/// </summary>
public static class App6DomainModifier
{
    public const string AirModifierClass = "map-app6-domain--air";
    public const string SurfaceModifierClass = "map-app6-domain--surface";
    public const string SubsurfaceModifierClass = "map-app6-domain--subsurface";
    public const string LandModifierClass = "map-app6-domain--land";
    public const string MineModifierClass = "map-app6-domain--mine";
    public const string FacilityModifierClass = "map-app6-domain--facility";

    /// <summary>2525-style battle-dimension character occupying SIDC position 3 (0-based index 2).</summary>
    public const char AirBattleDimension = 'A';
    public const char SurfaceBattleDimension = 'S';
    public const char SubsurfaceBattleDimension = 'U';
    public const char LandBattleDimension = 'G';
    public const char MineBattleDimension = 'X';
    public const char FacilityBattleDimension = 'I';

    public static IReadOnlyList<string> KnownDomainModifierClasses { get; } =
    [
        AirModifierClass,
        SurfaceModifierClass,
        SubsurfaceModifierClass,
        LandModifierClass,
        MineModifierClass,
        FacilityModifierClass,
    ];

    /// <summary>Resolved domain modifier: SIDC battle-dimension char, USS modifier class, unicode fallback icon.</summary>
    public sealed record DomainModifierResolution(char BattleDimension, string ModifierClass, string UnicodeIcon);

    private static readonly IReadOnlyDictionary<CombatDomain, DomainModifierResolution> DomainTable =
        new Dictionary<CombatDomain, DomainModifierResolution>
        {
            [CombatDomain.Air] = new(AirBattleDimension, AirModifierClass, "▲"),
            [CombatDomain.Surface] = new(SurfaceBattleDimension, SurfaceModifierClass, "—"),
            [CombatDomain.Subsurface] = new(SubsurfaceBattleDimension, SubsurfaceModifierClass, "▽"),
            [CombatDomain.Land] = new(LandBattleDimension, LandModifierClass, "●"),
            [CombatDomain.Mine] = new(MineBattleDimension, MineModifierClass, "✕"),
            [CombatDomain.Facility] = new(FacilityBattleDimension, FacilityModifierClass, "▭"),
        };

    /// <summary>Resolve the domain modifier (battle dimension char, USS class, fallback icon) for a combat domain.</summary>
    public static DomainModifierResolution Resolve(CombatDomain domain) =>
        DomainTable.TryGetValue(domain, out var resolution) ? resolution : DomainTable[CombatDomain.Land];

    /// <summary>
    /// Splice a domain's battle-dimension char into SIDC position 3 (0-based index 2), preserving the
    /// affiliation identity char at position 2 and all other positions. Pure — no allocation beyond the result.
    /// Returns <paramref name="affiliationSidc"/> unchanged when it is not a valid 15-char SIDC.
    /// </summary>
    public static string BuildSidc(string? affiliationSidc, CombatDomain domain)
    {
        if (!App6Sidc.IsValidSidc(affiliationSidc))
        {
            return affiliationSidc ?? App6Sidc.FallbackSidc;
        }

        var chars = affiliationSidc!.ToCharArray();
        chars[2] = Resolve(domain).BattleDimension;
        return new string(chars);
    }
}
