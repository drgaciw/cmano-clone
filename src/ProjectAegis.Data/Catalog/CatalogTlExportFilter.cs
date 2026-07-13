namespace ProjectAegis.Data.Catalog;

using ProjectAegis.Data.Platform;

/// <summary>
/// S30-02: read-only per-tier export slice over <see cref="PlatformCatalogExportData"/>.
/// Records at or below the requested TL ceiling are retained; sort keys follow S28-11 policy.
/// </summary>
public static class CatalogTlExportFilter
{
    public static PlatformCatalogExportData Apply(
        PlatformCatalogExportData data,
        string maxTlTier,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels = null)
    {
        var ceiling = CatalogTlTier.Normalize(maxTlTier);
        var gtlMap = platformGameTechnologyLevels ?? EmptyGtlMap;

        var sensors = CatalogTlExportSortKey.SortSensors(
            data.Sensors
                .Where(IsApprovedSensorBinding)
                .Where(row => CatalogTlTier.IsAtOrBelow(
                    CatalogTlTierResolver.ResolveFromSensor(
                        row,
                        gtlMap.TryGetValue(row.PlatformId, out var sensorGtl) ? sensorGtl : 0),
                    ceiling)),
            gtlMap);

        var allowedPlatforms = BuildAllowedPlatformIds(data, ceiling, gtlMap, sensors);

        var platforms = data.Platforms
            .Where(p => allowedPlatforms.Contains(p.PlatformId))
            .OrderBy(p => p.PlatformId, StringComparer.Ordinal)
            .ToArray();

        var mounts = CatalogSortKeyComparer.SortMounts(
            data.Mounts.Where(m => allowedPlatforms.Contains(m.PlatformId)));

        var loadouts = CatalogSortKeyComparer.SortLoadouts(
            data.Loadouts.Where(l => allowedPlatforms.Contains(l.PlatformId)));

        var magazines = CatalogSortKeyComparer.SortMagazines(
            data.Magazines.Where(m => allowedPlatforms.Contains(m.PlatformId)));

        var comms = CatalogTlExportSortKey.SortComms(
            data.Comms.Where(row => CatalogTlTier.IsAtOrBelow(
                CatalogTlTierResolver.ResolveFromComms(
                    row,
                    gtlMap.TryGetValue(row.PlatformId, out var commsGtl) ? commsGtl : 0),
                ceiling)),
            gtlMap);

        var mobility = data.Mobility == null
            ? null
            : CatalogTlExportSortKey.SortMobility(
                data.Mobility.Where(row => CatalogTlTier.IsAtOrBelow(
                    CatalogTlTierResolver.ResolveFromMobility(
                        row,
                        gtlMap.TryGetValue(row.PlatformId, out var mobilityGtl) ? mobilityGtl : 0),
                    ceiling)),
                gtlMap);

        var signatures = data.Signatures == null
            ? null
            : CatalogTlExportSortKey.SortSignatures(
                data.Signatures.Where(row => CatalogTlTier.IsAtOrBelow(
                    CatalogTlTierResolver.ResolveFromSignature(
                        row,
                        gtlMap.TryGetValue(row.PlatformId, out var signatureGtl) ? signatureGtl : 0),
                    ceiling)),
                gtlMap);

        var emcon = data.Emcon == null
            ? null
            : CatalogSortKeyComparer.SortEmcon(
                data.Emcon.Where(row => allowedPlatforms.Contains(row.PlatformId)));

        var damage = data.Damage == null
            ? null
            : CatalogTlExportSortKey.SortPlatformDamage(
                data.Damage.Where(row => CatalogTlTier.IsAtOrBelow(
                    CatalogTlTierResolver.ResolveFromPlatformDamage(
                        row,
                        gtlMap.TryGetValue(row.PlatformId, out var damageGtl) ? damageGtl : 0),
                    ceiling)),
                gtlMap);

        return new PlatformCatalogExportData(
            platforms,
            sensors,
            mounts,
            loadouts,
            magazines,
            comms,
            CatalogSortKeyComparer.SortLinks(data.Links ?? []),
            mobility,
            signatures,
            emcon,
            damage);
    }

    private static readonly IReadOnlyDictionary<string, int> EmptyGtlMap =
        new Dictionary<string, int>(StringComparer.Ordinal);

    private static bool IsApprovedSensorBinding(CatalogSensorBinding sensor) =>
        string.Equals(sensor.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static HashSet<string> BuildAllowedPlatformIds(
        PlatformCatalogExportData data,
        string ceiling,
        IReadOnlyDictionary<string, int> gtlMap,
        IReadOnlyList<CatalogSensorBinding> retainedSensors)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sensor in retainedSensors)
        {
            allowed.Add(sensor.PlatformId);
        }

        foreach (var platform in data.Platforms)
        {
            if (!gtlMap.TryGetValue(platform.PlatformId, out var gtl))
            {
                continue;
            }

            var tier = CatalogTlTierResolver.ResolveFromPlatform(gtl);
            if (CatalogTlTier.IsAtOrBelow(tier, ceiling))
            {
                allowed.Add(platform.PlatformId);
            }
        }

        return allowed;
    }
}