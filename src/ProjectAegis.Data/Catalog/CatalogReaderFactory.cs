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

    public static string ResolvePublicCorpusDatabasePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", CatalogValidationDefaults.PublicCorpusDatabaseFileName));

    public static string ResolveDatabasePathForDbRef(string? dbRef)
    {
        if (!string.IsNullOrWhiteSpace(dbRef) &&
            CatalogValidationDefaults.TryResolvePublicCorpusDbRef(dbRef.Trim(), out _))
        {
            return ResolvePublicCorpusDatabasePath();
        }

        return ResolveBalticPatrolDatabasePath();
    }

    /// <summary>
    /// Seeds <see cref="CatalogSeedBootstrap"/> when missing and returns a SQLite reader, or null when the repo root cannot be resolved.
    /// </summary>
    public static ICatalogReader? TryCreateBalticPatrolReader() =>
        TryCreateCatalogReaderAtPath(ResolveBalticPatrolDatabasePath(), CatalogSeedBootstrap.SeedBalticPatrol);

    public static ICatalogReader? TryCreateBalticV3Reader() =>
        TryCreateCatalogReaderAtPath(ResolveBalticPatrolDatabasePath(), CatalogSeedBootstrap.SeedBalticV3);

    /// <summary>Opens the enterprise public-corpus catalog (schema-only bootstrap when missing).</summary>
    public static ICatalogReader? TryCreatePublicCorpusReader() =>
        TryCreateCatalogReaderAtPath(ResolvePublicCorpusDatabasePath(), CatalogSeedBootstrap.EnsureSchemaOnly);

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

    private static ICatalogReader? TryCreateCatalogReaderAtPath(string dbPath, Action<string, bool> seed)
    {
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

            return new SqliteCatalogReader(dbPath, "harness-catalog");
        }
        catch
        {
            return null;
        }
    }
}