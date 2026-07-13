namespace ProjectAegis.Data.Catalog;

/// <summary>
/// S30-02 / S28-11: deterministic TL export diff sort keys
/// <c>(canonicalId, tlTier, valueTier)</c> ascending with <see cref="StringComparer.Ordinal"/>.
/// </summary>
public static class CatalogTlExportSortKey
{
    public static int Compare(string canonicalId, string tlTier, string valueTier, string otherCanonicalId, string otherTlTier, string otherValueTier)
    {
        var idCompare = string.Compare(canonicalId, otherCanonicalId, StringComparison.Ordinal);
        if (idCompare != 0)
        {
            return idCompare;
        }

        var tlCompare = string.Compare(tlTier, otherTlTier, StringComparison.Ordinal);
        if (tlCompare != 0)
        {
            return tlCompare;
        }

        return string.Compare(valueTier, otherValueTier, StringComparison.Ordinal);
    }

    public static string Format(string canonicalId, string tlTier, string valueTier) =>
        string.Join('\t', canonicalId, tlTier, valueTier);

    public static IReadOnlyList<CatalogSensorBinding> SortSensors(
        IEnumerable<CatalogSensorBinding> rows,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null) =>
        rows
            .Select(row => (
                Row: row,
                Key: Format(
                    CatalogSortKeyComparer.FormatSensorKey(row),
                    CatalogTlTierResolver.ResolveFromSensor(
                        row,
                        platformGameTechnologyLevels != null &&
                        platformGameTechnologyLevels.TryGetValue(row.PlatformId, out var gtl)
                            ? gtl
                            : 0),
                    row.ValueTier)))
            .OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Row)
            .ToArray();

    public static IReadOnlyList<CatalogCommsBinding> SortComms(
        IEnumerable<CatalogCommsBinding> rows,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null) =>
        rows
            .Select(row => (
                Row: row,
                Key: Format(
                    CatalogSortKeyComparer.FormatCommsKey(row),
                    CatalogTlTierResolver.ResolveFromComms(
                        row,
                        platformGameTechnologyLevels != null &&
                        platformGameTechnologyLevels.TryGetValue(row.PlatformId, out var gtl)
                            ? gtl
                            : 0),
                    row.ValueTier)))
            .OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Row)
            .ToArray();

    public static IReadOnlyList<CatalogMobility> SortMobility(
        IEnumerable<CatalogMobility> rows,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null) =>
        rows
            .Select(row => (
                Row: row,
                Key: Format(
                    CatalogSortKeyComparer.FormatMobilityKey(row),
                    CatalogTlTierResolver.ResolveFromMobility(
                        row,
                        platformGameTechnologyLevels != null &&
                        platformGameTechnologyLevels.TryGetValue(row.PlatformId, out var gtl)
                            ? gtl
                            : 0),
                    row.ValueTier)))
            .OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Row)
            .ToArray();

    public static IReadOnlyList<CatalogSignature> SortSignatures(
        IEnumerable<CatalogSignature> rows,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null) =>
        rows
            .Select(row => (
                Row: row,
                Key: Format(
                    CatalogSortKeyComparer.FormatSignatureKey(row),
                    CatalogTlTierResolver.ResolveFromSignature(
                        row,
                        platformGameTechnologyLevels != null &&
                        platformGameTechnologyLevels.TryGetValue(row.PlatformId, out var gtl)
                            ? gtl
                            : 0),
                    row.ValueTier)))
            .OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Row)
            .ToArray();

    public static IReadOnlyList<CatalogPlatformDamage> SortPlatformDamage(
        IEnumerable<CatalogPlatformDamage> rows,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null) =>
        rows
            .Select(row => (
                Row: row,
                Key: Format(
                    CatalogSortKeyComparer.FormatPlatformDamageKey(row),
                    CatalogTlTierResolver.ResolveFromPlatformDamage(
                        row,
                        platformGameTechnologyLevels != null &&
                        platformGameTechnologyLevels.TryGetValue(row.PlatformId, out var gtl)
                            ? gtl
                            : 0),
                    row.ValueTier)))
            .OrderBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Row)
            .ToArray();
}