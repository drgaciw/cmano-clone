using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Snapshots;

[Runner.CatalogSqliteCollection]
public sealed class DbSnapshotStoreTests
{
    [Test]
    public void Release_train_reads_sorted_releases()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var store = new DbSnapshotStore(dbPath);
            var releases = store.GetSortedReleases();
            Assert.That(releases.Any(r => r.ReleaseVersion == "catalog-p0-2026-06-04"), Is.True);
            Assert.That(store.GetSortedSnapshotIds().Any(id => id == CatalogValidationDefaults.BalticSnapshotId), Is.True);
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