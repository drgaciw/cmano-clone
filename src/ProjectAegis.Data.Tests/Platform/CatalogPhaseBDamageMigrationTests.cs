using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

public sealed class CatalogPhaseBDamageMigrationTests
{
    [Test]
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

    [Test]
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
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetDouble(0), Is.EqualTo(100d));
            Assert.That(reader.GetDouble(1), Is.EqualTo(25d));
            Assert.That(reader.GetInt32(2), Is.EqualTo(0));
            Assert.That(reader.GetString(3), Is.EqualTo(CatalogReviewStates.Provisional));
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

    [Test]
    public void CatalogPlatformDamage_record_defaults_match_schema()
    {
        var damage = new CatalogPlatformDamage("u1");
        Assert.That(damage.PlatformId, Is.EqualTo("u1"));
        Assert.That(damage.MaxHp, Is.EqualTo(100));
        Assert.That(damage.WithdrawThresholdPct, Is.EqualTo(0));
        Assert.That(damage.CriticalFlags, Is.EqualTo(0));
        Assert.That(damage.ReviewState, Is.EqualTo(CatalogReviewStates.Provisional));
        Assert.That(damage.TrlLevel, Is.EqualTo(9));
        Assert.That(damage.ValueTier, Is.EqualTo(CatalogProvenanceTier.GameplayAbstraction));
    }

    private static void AssertTableExists(string dbPath, string table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        Assert.That(Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
    }
}