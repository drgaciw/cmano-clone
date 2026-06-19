namespace ProjectAegis.Data.Catalog;

using ProjectAegis.Data.Osint;

/// <summary>
/// S30-02: infer per-record TL tier for export-only filtering (read-only path).
/// Uses <c>game_technology_level</c> when present, else OSINT branch tags + TRL bands per S28-11 spike.
/// </summary>
public static class CatalogTlTierResolver
{
    public static string ResolveFromProvenance(
        int gameTechnologyLevel,
        string? importBatchId,
        int trlLevel)
    {
        if (gameTechnologyLevel > 0)
        {
            return CatalogTlTier.FromGameTechnologyLevel(gameTechnologyLevel);
        }

        var batch = importBatchId ?? string.Empty;
        if (batch.StartsWith(OsintCatalogMapper.BranchTagPrefix + "10", StringComparison.Ordinal))
        {
            return trlLevel >= 7 ? CatalogTlTier.Tl5 : CatalogTlTier.Tl4;
        }

        if (batch.StartsWith(OsintCatalogMapper.BranchTagPrefix + "09", StringComparison.Ordinal))
        {
            if (trlLevel >= 8)
            {
                return CatalogTlTier.Tl3;
            }

            if (trlLevel >= 5)
            {
                return CatalogTlTier.Tl2;
            }

            return CatalogTlTier.Tl1;
        }

        return CatalogTlTier.Default;
    }

    public static string ResolveFromSensor(CatalogSensorBinding row, int platformGameTechnologyLevel = 0) =>
        ResolveFromProvenance(platformGameTechnologyLevel, row.ImportBatchId, row.TrlLevel);

    public static string ResolveFromComms(CatalogCommsBinding row, int platformGameTechnologyLevel = 0) =>
        ResolveFromProvenance(platformGameTechnologyLevel, string.Empty, row.TrlLevel);

    public static string ResolveFromMobility(CatalogMobility row, int platformGameTechnologyLevel = 0) =>
        ResolveFromProvenance(platformGameTechnologyLevel, string.Empty, row.TrlLevel);

    public static string ResolveFromSignature(CatalogSignature row, int platformGameTechnologyLevel = 0) =>
        ResolveFromProvenance(platformGameTechnologyLevel, string.Empty, row.TrlLevel);

    public static string ResolveFromPlatformDamage(CatalogPlatformDamage row, int platformGameTechnologyLevel = 0) =>
        ResolveFromProvenance(platformGameTechnologyLevel, string.Empty, row.TrlLevel);

    public static string ResolveFromPlatform(int gameTechnologyLevel) =>
        CatalogTlTier.FromGameTechnologyLevel(gameTechnologyLevel);

    public static string ResolveFromPlatformId(
        string platformId,
        IReadOnlyDictionary<string, int>? platformGameTechnologyLevels)
    {
        if (platformGameTechnologyLevels != null &&
            platformGameTechnologyLevels.TryGetValue(platformId, out var gtl))
        {
            return ResolveFromPlatform(gtl);
        }

        return CatalogTlTier.Default;
    }
}