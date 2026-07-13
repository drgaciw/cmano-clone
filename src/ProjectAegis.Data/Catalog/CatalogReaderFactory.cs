namespace ProjectAegis.Data.Catalog;

/// <summary>Resolves catalog readers for headless harness and CI (ADR-006 read path).</summary>
public static class CatalogReaderFactory
{
    public static string ResolveBalticPatrolDatabasePath()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        if (File.Exists(jsonPath))
        {
            return Path.Combine(Path.GetDirectoryName(jsonPath)!, "baltic_patrol.db");
        }

        return CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
    }

    /// <summary>
    /// Seeds <see cref="CatalogSeedBootstrap"/> when missing and returns a SQLite reader, or null when the repo root cannot be resolved.
    /// </summary>
    public static ICatalogReader? TryCreateBalticPatrolReader() =>
        TryCreateBalticPatrolReaderCore(CatalogSeedBootstrap.SeedBalticPatrol);

    public static ICatalogReader? TryCreateBalticV3Reader() =>
        TryCreateBalticPatrolReaderCore(CatalogSeedBootstrap.SeedBalticV3);

    public static ICatalogReader ResolveForScenario(string scenarioPolicyId, ICatalogReader? catalogOverride = null)
    {
        if (catalogOverride != null)
        {
            return catalogOverride;
        }

        if (IsBalticV3Scenario(scenarioPolicyId))
        {
            return InMemoryCatalogReader.BalticV3Fixture();
        }

        return TryCreateBalticPatrolReader() ?? InMemoryCatalogReader.BalticPatrolFixture();
    }

    public static bool IsBalticV3Scenario(string scenarioPolicyId) =>
        scenarioPolicyId.StartsWith("baltic-v3-", StringComparison.OrdinalIgnoreCase);

    private static ICatalogReader? TryCreateBalticPatrolReaderCore(Action<string, bool> seed)
    {
        var dbPath = ResolveBalticPatrolDatabasePath();
        if (!Path.IsPathRooted(dbPath) && !File.Exists(dbPath))
        {
            return null;
        }

        dbPath = Path.GetFullPath(dbPath);

        try
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(dbPath))
            {
                seed(dbPath, true);
            }

            return new SqliteCatalogReader(dbPath, "harness-baltic");
        }
        catch
        {
            return null;
        }
    }
}