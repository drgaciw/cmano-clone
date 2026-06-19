namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Req-06 TL-0–TL-5 tier labels for export metadata and <c>catalog_snapshot.branch</c>.
/// Export-only in S29-02 — no runtime branch DB binding.
/// </summary>
public static class CatalogTlTier
{
    public const string Tl0 = "TL-0";
    public const string Tl1 = "TL-1";
    public const string Tl2 = "TL-2";
    public const string Tl3 = "TL-3";
    public const string Tl4 = "TL-4";
    public const string Tl5 = "TL-5";

    public const string Default = Tl0;

    /// <summary>Latest applied catalog migration id for export manifest <c>schemaVersion</c>.</summary>
    public const string CatalogSchemaVersion = "010";

    /// <summary>Export drop manifest schema revision (distinct from workbook <c>SchemaVersion</c>).</summary>
    public const string ExportManifestSchemaVersion = "1";

    public static readonly IReadOnlyList<string> All =
    [
        Tl0,
        Tl1,
        Tl2,
        Tl3,
        Tl4,
        Tl5,
    ];

    public static bool IsValid(string? tier) =>
        !string.IsNullOrWhiteSpace(tier) && All.Contains(tier.Trim(), StringComparer.Ordinal);

    public static string Normalize(string? tier) => IsValid(tier) ? tier!.Trim() : Default;

    /// <summary>Ordinal index for TL-0…TL-5 (0-based). Unknown tiers map to TL-0.</summary>
    public static int ToOrdinal(string? tier)
    {
        var normalized = Normalize(tier);
        for (var i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i], normalized, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }

    public static string FromOrdinal(int ordinal) =>
        All[Math.Clamp(ordinal, 0, All.Count - 1)];

    /// <summary>True when <paramref name="recordTier"/> is at or below the export ceiling <paramref name="maxTier"/>.</summary>
    public static bool IsAtOrBelow(string? recordTier, string? maxTier) =>
        ToOrdinal(recordTier) <= ToOrdinal(maxTier);

    /// <summary>Clamp game technology level (0–5) to a TL label.</summary>
    public static string FromGameTechnologyLevel(int gameTechnologyLevel) =>
        FromOrdinal(Math.Clamp(gameTechnologyLevel, 0, 5));
}