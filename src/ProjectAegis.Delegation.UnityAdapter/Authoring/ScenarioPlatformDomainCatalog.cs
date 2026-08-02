namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

/// <summary>
/// Domain-grouped platform presets for Scenario Map Authoring menu navigation.
/// Catalog entries use Baltic/gauntlet platform ids already present in scenario fixtures
/// so place-unit commits stay consistent with headless content.
/// </summary>
public static class ScenarioPlatformDomainCatalog
{
    public const string DomainAircraft = "aircraft";
    public const string DomainShips = "ships";
    public const string DomainSubs = "subs";
    public const string DomainGround = "ground";

    private static readonly ScenarioPlatformPreset[] Aircraft =
    {
        new(DomainAircraft, "jas-39c-gripen-2005", "JAS 39C Gripen (2005)", "blue"),
        new(DomainAircraft, "f-16c-block-50", "F-16C Block 50", "blue"),
        new(DomainAircraft, "su-27sm-flanker", "Su-27SM Flanker", "red"),
        new(DomainAircraft, "su-30sm", "Su-30SM", "red"),
        new(DomainAircraft, "e-3a-sentry", "E-3A Sentry", "blue"),
    };

    private static readonly ScenarioPlatformPreset[] Ships =
    {
        new(DomainShips, "k-31-visby-2009", "K31 Visby (2009)", "blue"),
        new(DomainShips, "k-22-gavle-ex-goteborg-class", "K22 Gävle", "blue"),
        new(DomainShips, "em-sovremenny-i-pr-956-sarych", "Sovremenny I (956)", "red"),
        new(DomainShips, "mpk-steregushchiy-pr-20380-2018", "Steregushchiy (20380)", "red"),
        new(DomainShips, "ffg", "Generic FFG", "blue"),
        new(DomainShips, "ddg", "Generic DDG", "red"),
    };

    private static readonly ScenarioPlatformPreset[] Subs =
    {
        new(DomainSubs, "a-19-gotland-2022", "A19 Gotland (2022)", "blue"),
        new(DomainSubs, "ssn", "Generic SSN", "red"),
        new(DomainSubs, "ssk", "Generic SSK", "blue"),
        new(DomainSubs, "kilo-pr-636", "Kilo (636)", "red"),
    };

    private static readonly ScenarioPlatformPreset[] Ground =
    {
        new(DomainGround, "sam-patriot-pac3", "Patriot PAC-3 battery", "blue"),
        // Swedish Army LVS-103 battery (CMANO facility/3434); UI label matches user/catalog naming.
        new(DomainGround, "m901-patriot-pac3-erint-mse", "M901 Patriot [PAC-3 ERINT/MSE]", "blue"),
        new(DomainGround, "sam-s-400", "S-400 battery", "red"),
        new(DomainGround, "coastal-rbs15", "RBS-15 coastal battery", "blue"),
        new(DomainGround, "facility-airbase", "Air base (facility)", "blue"),
        new(DomainGround, "facility-port", "Port facility", "red"),
    };

    /// <summary>Stable domain ids for menu navigation (aircraft, ships, subs, ground).</summary>
    public static IReadOnlyList<string> Domains { get; } =
        new[] { DomainAircraft, DomainShips, DomainSubs, DomainGround };

    /// <summary>Human labels for domain buttons.</summary>
    public static string DomainLabel(string domainId) => domainId switch
    {
        DomainAircraft => "Aircraft",
        DomainShips => "Ships",
        DomainSubs => "Subs",
        DomainGround => "Ground",
        _ => domainId,
    };

    /// <summary>Presets for a domain id (empty when unknown).</summary>
    public static IReadOnlyList<ScenarioPlatformPreset> GetPresets(string domainId)
    {
        if (string.IsNullOrWhiteSpace(domainId))
        {
            return Array.Empty<ScenarioPlatformPreset>();
        }

        return domainId.Trim().ToLowerInvariant() switch
        {
            DomainAircraft or "air" or "plane" or "planes" => Aircraft,
            DomainShips or "ship" or "surface" or "naval" => Ships,
            DomainSubs or "sub" or "submarine" or "submarines" or "subsurface" => Subs,
            DomainGround or "land" or "facility" or "facilities" => Ground,
            _ => Array.Empty<ScenarioPlatformPreset>(),
        };
    }

    /// <summary>All presets across domains (stable domain order, then catalog order).</summary>
    public static IReadOnlyList<ScenarioPlatformPreset> AllPresets()
    {
        var list = new List<ScenarioPlatformPreset>(Aircraft.Length + Ships.Length + Subs.Length + Ground.Length);
        list.AddRange(Aircraft);
        list.AddRange(Ships);
        list.AddRange(Subs);
        list.AddRange(Ground);
        return list;
    }

    /// <summary>Resolve a preset by platform id (case-insensitive); null when unknown.</summary>
    public static ScenarioPlatformPreset? FindByPlatformId(string platformId)
    {
        if (string.IsNullOrWhiteSpace(platformId))
        {
            return null;
        }

        foreach (var preset in AllPresets())
        {
            if (string.Equals(preset.PlatformId, platformId, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }

        return null;
    }

    /// <summary>Suggest a unique unit id for a platform (e.g. u-visby-1).</summary>
    public static string SuggestUnitId(string platformId, int sequence = 1)
    {
        var slug = string.IsNullOrWhiteSpace(platformId) ? "unit" : platformId.Trim().ToLowerInvariant();
        if (slug.Length > 24)
        {
            slug = slug[..24];
        }

        return $"u-{slug}-{Math.Max(1, sequence)}";
    }
}

/// <summary>One selectable platform preset under a scenario authoring domain.</summary>
public sealed class ScenarioPlatformPreset
{
    public ScenarioPlatformPreset(string domainId, string platformId, string displayName, string defaultSideId)
    {
        DomainId = domainId ?? throw new ArgumentNullException(nameof(domainId));
        PlatformId = platformId ?? throw new ArgumentNullException(nameof(platformId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        DefaultSideId = defaultSideId ?? "blue";
    }

    public string DomainId { get; }

    public string PlatformId { get; }

    public string DisplayName { get; }

    public string DefaultSideId { get; }

    /// <summary>List row text: display name + platform id.</summary>
    public string ListLine => $"{DisplayName}  [{PlatformId}]";
}
