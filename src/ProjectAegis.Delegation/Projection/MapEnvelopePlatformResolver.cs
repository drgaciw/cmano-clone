namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Maps scenario unit ids to catalog platform ids for envelope range lookup (DRG-160 / #495).
/// Uses existing <see cref="ICatalogReader"/> surface only — no ORBAT or catalog API widening.
/// </summary>
public static class MapEnvelopePlatformResolver
{
    /// <summary>
    /// Resolves the catalog platform id for <paramref name="unitId"/>.
    /// When the unit id already matches catalog sensor/combat-radius data, returns it unchanged.
    /// When the unit id is a suffixed instance (e.g. <c>u1-alpha</c> for platform <c>u1</c>),
    /// returns the longest matching catalog platform prefix.
    /// </summary>
    public static string? Resolve(ICatalogReader? catalog, string? unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }

        if (catalog is null)
        {
            return unitId;
        }

        if (CatalogEnvelopeRangeResolver.TryResolveSensorRangeNm(catalog, unitId, out _))
        {
            return unitId;
        }

        if (catalog.TryGetCombatRadiusNm(unitId, out _))
        {
            return unitId;
        }

        var prefixMatch = FindLongestPlatformPrefix(catalog, unitId);
        return prefixMatch ?? unitId;
    }

    private static string? FindLongestPlatformPrefix(ICatalogReader catalog, string unitId)
    {
        string? best = null;
        foreach (var mobility in catalog.GetSortedMobility())
        {
            if (mobility is null || string.IsNullOrWhiteSpace(mobility.PlatformId))
            {
                continue;
            }

            var platformId = mobility.PlatformId;
            if (string.Equals(platformId, unitId, StringComparison.Ordinal))
            {
                return platformId;
            }

            if (unitId.StartsWith(platformId + "-", StringComparison.Ordinal)
                && (best is null || platformId.Length > best.Length))
            {
                best = platformId;
            }
        }

        foreach (var mount in catalog.GetSortedMounts())
        {
            if (mount is null || string.IsNullOrWhiteSpace(mount.PlatformId))
            {
                continue;
            }

            var platformId = mount.PlatformId;
            if (string.Equals(platformId, unitId, StringComparison.Ordinal))
            {
                return platformId;
            }

            if (unitId.StartsWith(platformId + "-", StringComparison.Ordinal)
                && (best is null || platformId.Length > best.Length))
            {
                best = platformId;
            }
        }

        return best;
    }
}
