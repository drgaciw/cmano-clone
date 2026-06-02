using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

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
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
            Assert.Equal(1.0, basePd);
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