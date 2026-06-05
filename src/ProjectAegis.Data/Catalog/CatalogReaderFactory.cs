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
    public static ICatalogReader? TryCreateBalticPatrolReader()
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
                CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            }

            return new SqliteCatalogReader(dbPath, "harness-baltic");
        }
        catch
        {
            return null;
        }
    }
}