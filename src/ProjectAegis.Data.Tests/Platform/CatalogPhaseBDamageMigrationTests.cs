using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

public sealed class CatalogPhaseBDamageMigrationTests
{
    [Fact]
    public void Migration_009_applies_idempotently()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-damage-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            AssertTableExists(dbPath, "platform_damage");
            AssertTableExists(dbPath, "catalog_staging_damage");

            using (var reader = new SqliteCatalogReader(dbPath, "phase-b-damage-idempotent"))
            {
                _ = reader.GetSortedSensorBindings();
            }

            AssertTableExists(dbPath, "platform_damage");
            AssertTableExists(dbPath, "platform_mobility");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void Baltic_seed_inserts_u1_damage_row()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-baltic-damage-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            AssertTableExists(dbPath, "platform_damage");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT max_hp, withdraw_threshold_pct, critical_flags, review_state
                FROM platform_damage
                WHERE platform_id = 'u1'
                """;
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(100d, reader.GetDouble(0));
            Assert.Equal(25d, reader.GetDouble(1));
            Assert.Equal(0, reader.GetInt32(2));
            Assert.Equal(CatalogReviewStates.Provisional, reader.GetString(3));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void CatalogPlatformDamage_record_defaults_match_schema()
    {
        var damage = new CatalogPlatformDamage("u1");
        Assert.Equal("u1", damage.PlatformId);
        Assert.Equal(100, damage.MaxHp);
        Assert.Equal(0, damage.WithdrawThresholdPct);
        Assert.Equal(0, damage.CriticalFlags);
        Assert.Equal(CatalogReviewStates.Provisional, damage.ReviewState);
        Assert.Equal(9, damage.TrlLevel);
        Assert.Equal(CatalogProvenanceTier.GameplayAbstraction, damage.ValueTier);
    }

    private static void AssertTableExists(string dbPath, string table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
    }
}