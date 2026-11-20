using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CatalogSeedBootstrapTests
{
    [Test]
    public void SeedBalticPatrol_writes_sorted_sqlite_catalog()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-seed-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "p0-seed-test");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var radar1), Is.True);
            Assert.That(radar1, Is.EqualTo(1.0));
            Assert.That(reader.TryGetBasePd("u1", "radar-2", out var radar2), Is.True);
            Assert.That(radar2, Is.EqualTo(0.75));
            // Baltic radar pair + SWARM-A1 generic swarm EO/IR sensor.
            Assert.That(reader.GetSortedSensorBindings().Count >= 2, Is.True);
            Assert.That(reader.TryGetBasePd(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                CatalogSwarmPlatformDefaults.DefaultSensorId,
                out _), Is.True);
        }
        finally
        {
            SqliteConnectionClear(dbPath);
        }
    }

    private static void SqliteConnectionClear(string dbPath)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
