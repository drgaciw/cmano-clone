using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CatalogSeedBootstrapTests
{
    [Fact]
    public void SeedBalticPatrol_writes_sorted_sqlite_catalog()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-seed-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "p0-seed-test");
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var radar1));
            Assert.Equal(1.0, radar1);
            Assert.True(reader.TryGetBasePd("u1", "radar-2", out var radar2));
            Assert.Equal(0.75, radar2);
            // Baltic radar pair + SWARM-A1 generic swarm EO/IR sensor.
            Assert.True(reader.GetSortedSensorBindings().Count >= 2);
            Assert.True(reader.TryGetBasePd(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                CatalogSwarmPlatformDefaults.DefaultSensorId,
                out _));
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
