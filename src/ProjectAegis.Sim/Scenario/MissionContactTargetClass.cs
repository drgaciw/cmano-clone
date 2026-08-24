namespace ProjectAegis.Sim.Scenario;

public enum MissionContactTargetClass
{
    Any = 0,
    Surface = 1,
    Air = 2,
    Subsurface = 3,
}

public static class MissionContactTargetClassifier
{
    /// <summary>
    /// Catalog <c>platform.domain</c> tokens used by gauntlet/baltic platform ids.
    /// ICatalogReader does not yet expose domain; these tokens are the catalog domain
    /// values those ids carry. Baltic v3 <c>ucav-*</c> stays Air for ReplayGolden.
    /// </summary>
    private static readonly string[] CatalogSubsurfaceDomainTokens =
    [
        "ssn", "ssk", "ssbn", "ssgn", "kilo", "gotland", "yasen", "virginia",
        "akula", "oscar", "astute", "collins", "type-212", "type-214", "uuv",
    ];

    private static readonly string[] CatalogAirDomainTokens =
    [
        "ucav", "jas-39", "eurofighter", "tu-160", "tu-22", "tu-95",
        "blackjack", "mig-", "su-27", "su-30", "su-33", "su-34", "su-35", "su-57",
        "flanker", "felon", "fullback", "f-16", "f-15", "f-18", "f-22", "f-35",
        "ka-27", "ka-52", "helix", "ah-64", "e-3a", "e-3-", "gripen", "falcon",
        "foxhound", "fulcrum", "sentry",
    ];

    public static MissionContactTargetClass FromCatalogDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return MissionContactTargetClass.Surface;
        }

        return domain.Trim().ToLowerInvariant() switch
        {
            "air" or "aircraft" => MissionContactTargetClass.Air,
            "subsurface" or "submarine" or "sub" or "subs" => MissionContactTargetClass.Subsurface,
            _ => MissionContactTargetClass.Surface,
        };
    }

    public static MissionContactTargetClass Classify(string targetId) =>
        FromCatalogDomain(ResolveCatalogDomain(targetId));

    public static string ResolveCatalogDomain(string targetId)
    {
        if (ContainsToken(targetId, CatalogSubsurfaceDomainTokens))
        {
            return "subsurface";
        }

        if (ContainsToken(targetId, CatalogAirDomainTokens))
        {
            return "air";
        }

        return "surface";
    }

    public static bool Matches(MissionContactTargetClass required, string targetId) =>
        required switch
        {
            MissionContactTargetClass.Any => true,
            MissionContactTargetClass.Surface => Classify(targetId) == MissionContactTargetClass.Surface,
            MissionContactTargetClass.Air => Classify(targetId) == MissionContactTargetClass.Air,
            MissionContactTargetClass.Subsurface => Classify(targetId) == MissionContactTargetClass.Subsurface,
            _ => false,
        };

    private static bool ContainsToken(string targetId, string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (targetId.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
