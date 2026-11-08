namespace ProjectAegis.Data.Snapshots;

/// <summary>S32-02: canonical nightly corpus domains for unified release-train manifest consolidation.</summary>
public static class CatalogReleaseTrainDomains
{
    public const string Sensor = "sensor";
    public const string Weapon = "weapon";
    public const string Platform = "platform";
    public const string Aircraft = "aircraft";
    public const string Submarine = "submarine";
    public const string Facility = "facility";

    public static readonly IReadOnlyList<string> All =
    [
        Sensor,
        Weapon,
        Platform,
        Aircraft,
        Submarine,
        Facility,
    ];

    public static bool IsKnown(string? domain) =>
        !string.IsNullOrWhiteSpace(domain) &&
        All.Contains(domain.Trim(), StringComparer.Ordinal);

    /// <summary>Extract domain from nightly release versions of flexible shape "nightly-&lt;known-domain&gt;-&lt;corpus-variant&gt;-..." (S31+ variants, per release-train-scope-boundary-2026-06-24.md and S65 review).</summary>
    public static bool TryParseFromReleaseVersion(string? releaseVersion, out string domain)
    {
        domain = "";
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            return false;
        }

        var trimmed = releaseVersion.Trim();
        if (!trimmed.StartsWith("nightly-", StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = trimmed["nightly-".Length..];
        var dash = remainder.IndexOf('-', StringComparison.Ordinal);
        if (dash <= 0)
        {
            return false;
        }

        var candidate = remainder[..dash];
        if (!IsKnown(candidate))
        {
            return false;
        }

        domain = candidate;
        return true;
    }
}