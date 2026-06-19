namespace ProjectAegis.Data.Platform;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 / PLE-1.3: resolve <see cref="PlatformCatalogExportData"/> from a bound catalog snapshot.
/// Keeps CLI verbs decoupled from raw SQLite wiring while returning real DB payloads when a snapshot resolves.
/// </summary>
public static class PlatformCatalogExportResolver
{
    public static bool TryResolve(
        string? dbPath,
        string snapshotId,
        out PlatformCatalogExportData data,
        string? maxTlTier = null)
    {
        data = PlatformCatalogExportData.Empty;
        if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
        {
            return false;
        }

        using var reader = new SqliteCatalogReader(dbPath, "cli-export-resolver");
        if (!reader.TryResolveDbRef(snapshotId, out _))
        {
            return false;
        }

        data = reader.LoadExportData(maxTlTier);
        return true;
    }
}