namespace ProjectAegis.Data.Catalog;

/// <summary>Baltic harness platform rows for validation when SQLite platform table is empty.</summary>
public static class CatalogValidationDefaults
{
    public const string BalticSnapshotId = "baltic_patrol";

    /// <summary>Enterprise public-corpus catalog (docs/reference/cmano-db → write gate).</summary>
    public const string PublicCorpusSnapshotId = "aegis_public_corpus";

    public const string PublicCorpusDatabaseFileName = "aegis_public_corpus.db";

    /// <summary>
    /// Placeholder platform for standalone CMO sensor catalog rows (sensor.md import).
    /// Keeps kill-chain PlatformToSensor edges resolvable before ship/aircraft fittings load.
    /// </summary>
    public const string PublicCorpusSensorCatalogPlatformId = "cmo-sensor-catalog";

    public static IReadOnlyList<CatalogPlatformEntry> BalticPlatforms() =>
    [
        new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
        new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
        new CatalogPlatformEntry("hostile-far", 65.0, 35.0, 200.0),
    ];

    /// <summary>Baltic v3 OOB: patrol ships plus one UCAV per side (Recon [Internal IR]),
    /// plus one attack submarine per side (Virginia-class-derived stats/sensors; QA-gauntlet
    /// Tier-3 fixture addition, see production/qa/gauntlet/gauntlet-20260709-1242/tier-3/).</summary>
    public static IReadOnlyList<CatalogPlatformEntry> BalticV3Platforms() =>
    [
        new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
        new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
        new CatalogPlatformEntry("hostile-far", 65.0, 35.0, 200.0),
        new CatalogPlatformEntry("ucav-blue", 57.2, 19.8, 180.0),
        new CatalogPlatformEntry("ucav-red", 58.3, 21.2, 180.0),
        new CatalogPlatformEntry("usub-blue", 57.1, 19.9, 500.0),
        new CatalogPlatformEntry("usub-red", 58.4, 21.1, 500.0),
    ];

    /// <summary>Baltic comms FK targets for link_catalog seeding (S34-02).</summary>
    public static IReadOnlyList<CatalogLinkEntry> BalticLinks() =>
    [
        new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
        new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, LatencyMsNominal: 250),
    ];

    public static bool TryResolvePublicCorpusDbRef(string dbRef, out string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(dbRef))
        {
            snapshotId = "";
            return false;
        }

        if (string.Equals(dbRef, PublicCorpusSnapshotId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dbRef, "public-corpus", StringComparison.OrdinalIgnoreCase) ||
            dbRef.Contains("public-corpus", StringComparison.OrdinalIgnoreCase) ||
            dbRef.Contains("aegis_public_corpus", StringComparison.OrdinalIgnoreCase))
        {
            snapshotId = PublicCorpusSnapshotId;
            return true;
        }

        snapshotId = "";
        return false;
    }

    public static bool TryResolveBalticDbRef(string dbRef, out string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(dbRef))
        {
            snapshotId = "";
            return false;
        }

        if (TryResolvePublicCorpusDbRef(dbRef, out snapshotId))
        {
            return false;
        }

        if (string.Equals(dbRef, BalticSnapshotId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dbRef, "baltic-patrol", StringComparison.OrdinalIgnoreCase) ||
            dbRef.Contains("baltic", StringComparison.OrdinalIgnoreCase))
        {
            snapshotId = BalticSnapshotId;
            return true;
        }

        snapshotId = "";
        return false;
    }
}