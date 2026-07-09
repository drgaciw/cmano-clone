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
            Assert.Equal(2, reader.GetSortedSensorBindings().Count);
        }
        finally
        {
            SqliteConnectionClear(dbPath);
        }
    }

    [Fact]
    public void SeedBalticPatrol_includes_weapon_mount_loadout_for_u1_without_sensor_orphans()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-seed-engage-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            Assert.True(ScalarCount(connection, "SELECT COUNT(*) FROM weapon_catalog") >= 1);
            Assert.True(ScalarCount(connection, "SELECT COUNT(*) FROM platform_mount") >= 1);
            Assert.True(ScalarCount(connection, "SELECT COUNT(*) FROM platform_loadout") >= 1);
            Assert.True(ScalarCount(connection, "SELECT COUNT(*) FROM platform_magazine") >= 1);
            Assert.Equal(
                0,
                ScalarCount(
                    connection,
                    """
                    SELECT COUNT(*) FROM sensor s
                    WHERE NOT EXISTS (SELECT 1 FROM platform p WHERE p.platform_id = s.platform_id)
                    """));
            Assert.Equal(
                0,
                ScalarCount(
                    connection,
                    """
                    SELECT COUNT(*) FROM platform_mount m
                    WHERE NOT EXISTS (SELECT 1 FROM platform p WHERE p.platform_id = m.platform_id)
                    """));

            using var reader = new SqliteCatalogReader(dbPath, "p0-seed-engage");
            Assert.True(reader.TryGetWeaponEnvelope(CatalogWeaponIds.BalticRim66, out var rim));
            Assert.Equal(74_000, rim.MaxRangeMeters);
            Assert.Contains(reader.GetSortedMounts(), m => m.PlatformId == "u1" && m.MountId == "vls-fwd");
        }
        finally
        {
            SqliteConnectionClear(dbPath);
        }
    }

    private static int ScalarCount(Microsoft.Data.Sqlite.SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
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