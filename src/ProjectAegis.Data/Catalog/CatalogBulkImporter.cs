namespace ProjectAegis.Data.Catalog;

/// <summary>Imports every <c>*.json</c> sensor drop in a directory into one SQLite catalog (CMO pipeline MVP).</summary>
public static class CatalogBulkImporter
{
    public static int ImportDirectory(string directoryPath, string databasePath, bool overwrite = true)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Catalog import directory not found: {directoryPath}");
        }

        var files = Directory
            .EnumerateFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            throw new InvalidOperationException($"No catalog JSON files in {directoryPath}");
        }

        var merged = new Dictionary<(string Platform, string Sensor), CatalogSensorBinding>();
        foreach (var file in files)
        {
            foreach (var binding in CatalogJsonImporter.ReadSensorBindings(file))
            {
                merged[(binding.PlatformId, binding.SensorId)] = binding;
            }
        }

        var (approved, quarantined) = CatalogImportGate.PartitionForImport(merged.Values);
        CatalogJsonImporter.WriteSqlite(databasePath, approved, overwrite);
        if (quarantined.Length > 0)
        {
            CatalogJsonImporter.WriteQuarantineRows(databasePath, quarantined);
        }
        return files.Length;
    }

    public static string ResolveCatalogImportDirectory() =>
        CatalogJsonImporter.ResolveRepoRelative(Path.Combine("assets", "data", "catalog", "import"));
}