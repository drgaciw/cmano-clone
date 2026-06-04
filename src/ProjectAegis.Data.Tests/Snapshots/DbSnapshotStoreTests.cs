using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class DbSnapshotStoreTests
{
    [Fact]
    public void Release_train_reads_sorted_releases()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var store = new DbSnapshotStore(dbPath);
            var releases = store.GetSortedReleases();
            Assert.Contains(releases, r => r.ReleaseVersion == "catalog-p0-2026-06-04");
            Assert.Contains(store.GetSortedSnapshotIds(), id => id == CatalogValidationDefaults.BalticSnapshotId);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}