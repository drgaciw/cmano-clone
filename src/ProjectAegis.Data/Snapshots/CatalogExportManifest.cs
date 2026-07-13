namespace ProjectAegis.Data.Snapshots;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Export drop manifest per S28-11 spike:
/// <c>{ dbVersion, tlTier, schemaVersion, contentHash, exportSchemaVersion }</c>.
/// Metadata only — no runtime scenario branch binding.
/// </summary>
public sealed record CatalogExportManifest(
    string DbVersion,
    string TlTier,
    string SchemaVersion,
    string ContentHash,
    string ExportSchemaVersion)
{
    public static CatalogExportManifest DefaultForSnapshot(string snapshotId) =>
        new(
            DbVersion: snapshotId,
            TlTier: CatalogTlTier.Default,
            SchemaVersion: CatalogTlTier.CatalogSchemaVersion,
            ContentHash: string.Empty,
            ExportSchemaVersion: CatalogTlTier.ExportManifestSchemaVersion);

    public static CatalogExportManifest Resolve(
        string databasePath,
        string snapshotId,
        string? releaseVersion = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
        {
            return DefaultForSnapshot(snapshotId);
        }

        using var store = new DbSnapshotStore(databasePath);
        var tlTier = store.TryGetBranch(snapshotId, out var branch)
            ? branch
            : CatalogTlTier.Default;
        _ = store.TryGetContentHash(snapshotId, out var contentHash);

        var dbVersion = !string.IsNullOrWhiteSpace(releaseVersion)
            ? releaseVersion.Trim()
            : ResolveReleaseVersion(store, snapshotId) ?? snapshotId;

        if (store.TryGetUnifiedManifest(dbVersion, out var unified))
        {
            return unified.ToExportManifest();
        }

        return new CatalogExportManifest(
            dbVersion,
            tlTier,
            CatalogTlTier.CatalogSchemaVersion,
            contentHash,
            CatalogTlTier.ExportManifestSchemaVersion);
    }

    private static string? ResolveReleaseVersion(DbSnapshotStore store, string snapshotId)
    {
        foreach (var release in store.GetSortedReleases())
        {
            if (string.Equals(release.SnapshotId, snapshotId, StringComparison.Ordinal))
            {
                return release.ReleaseVersion;
            }
        }

        return null;
    }
}