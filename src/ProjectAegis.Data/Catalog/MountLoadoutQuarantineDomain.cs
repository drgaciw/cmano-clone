namespace ProjectAegis.Data.Catalog;

/// <summary>S32-03: nightly corpus domains for mount/loadout child-row quarantine triage.</summary>
public static class MountLoadoutQuarantineDomain
{
    public const string Platform = "platform";
    public const string Submarine = "submarine";
    public const string Facility = "facility";

    public static readonly IReadOnlyList<string> ChildRowDomains =
    [
        Platform,
        Submarine,
        Facility,
    ];

    public static bool IsKnown(string? domain) =>
        !string.IsNullOrWhiteSpace(domain) &&
        ChildRowDomains.Contains(domain.Trim(), StringComparer.Ordinal);

    /// <summary>Map <c>platform.domain</c> values to nightly entity labels.</summary>
    public static string FromPlatformDomain(string? platformDomain) =>
        platformDomain?.Trim().ToLowerInvariant() switch
        {
            "subsurface" => Submarine,
            "land" => Facility,
            "surface" => Platform,
            _ => Platform,
        };

    public static string FromEntityHint(string? entityHint)
    {
        if (string.IsNullOrWhiteSpace(entityHint))
        {
            return Platform;
        }

        var normalized = entityHint.Trim().ToLowerInvariant();
        return normalized switch
        {
            "ship" or "platform" or "platforms" => Platform,
            "submarine" or "submarines" => Submarine,
            "facility" or "facilities" => Facility,
            _ when IsKnown(normalized) => normalized,
            _ => Platform,
        };
    }
}