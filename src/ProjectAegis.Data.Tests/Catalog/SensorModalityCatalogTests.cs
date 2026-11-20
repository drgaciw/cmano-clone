using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S111-02 / DRG-10: catalog-extend sensor modality (Radar default + IR/Visual fixtures).</summary>
[Runner.CatalogSqliteCollection]
public sealed class SensorModalityCatalogTests
{
    [Test]
    public void Migration_016_adds_modality_column_and_skip_is_safe()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-modality-mig-{Guid.NewGuid():N}.db");
        try
        {
            using (new SqliteCatalogReader(dbPath, "s111-modality-mig"))
            {
                Assert.That(ColumnExists(dbPath, "sensor", "modality"), Is.True);
            }

            // Second open must skip re-ALTER (ShouldSkipMigration) without error.
            using (var reader = new SqliteCatalogReader(dbPath, "s111-modality-mig-reopen"))
            {
                Assert.That(ColumnExists(dbPath, "sensor", "modality"), Is.True);
                _ = reader.GetSortedSensorBindings();
            }
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Test]
    public void Seed_and_reader_expose_ir_and_visual_fixtures_with_radar_default()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-modality-seed-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "s111-modality-seed");
            var byId = reader.GetSortedSensorBindings()
                .Where(b => string.Equals(b.PlatformId, "u1", StringComparison.Ordinal))
                .ToDictionary(b => b.SensorId, StringComparer.Ordinal);

            Assert.That(byId.TryGetValue("fixture-ir-1", out var ir), Is.True);
            Assert.That(ir!.Modality, Is.EqualTo(CatalogSensorModalities.Infrared));
            Assert.That(ir.BasePd, Is.EqualTo(0.80));

            Assert.That(byId.TryGetValue("fixture-visual-1", out var visual), Is.True);
            Assert.That(visual!.Modality, Is.EqualTo(CatalogSensorModalities.Visual));
            Assert.That(visual.BasePd, Is.EqualTo(0.70));

            Assert.That(byId.TryGetValue("radar-1", out var radar), Is.True);
            Assert.That(radar!.Modality, Is.EqualTo(CatalogSensorModalities.Radar));
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Test]
    public void InMemory_default_modality_is_radar_and_v3_ir_tagged()
    {
        var patrol = InMemoryCatalogReader.BalticPatrolFixture();
        var radar = patrol.GetSortedSensorBindings()
            .Single(b => b.PlatformId == "u1" && b.SensorId == "radar-1");
        Assert.That(radar.Modality, Is.EqualTo(CatalogSensorModalities.Radar));

        var v3 = InMemoryCatalogReader.BalticV3Fixture();
        var ir = v3.GetSortedSensorBindings()
            .Single(b => b.PlatformId == "ucav-blue" && b.SensorId == "internal-ir");
        Assert.That(ir.Modality, Is.EqualTo(CatalogSensorModalities.Infrared));
    }

    [Test]
    public void Reader_defaults_null_modality_to_radar()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-modality-null-{Guid.NewGuid():N}.db");
        try
        {
            using (new SqliteCatalogReader(dbPath, "s111-null-mod"))
            {
                using var insert = new SqliteConnection($"Data Source={dbPath};Pooling=false");
                insert.Open();
                using var cmd = insert.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                        import_batch_id, source_file, review_state, trl_level, modality)
                    VALUES ('u1', 'legacy-null', 0.5, 'test', 1.0, '', '', 'approved', 9, '')
                    """;
                cmd.ExecuteNonQuery();
            }

            using var reader = new SqliteCatalogReader(dbPath, "s111-null-mod-read");
            var binding = reader.GetSortedSensorBindings()
                .Single(b => b.SensorId == "legacy-null");
            Assert.That(binding.Modality, Is.EqualTo(CatalogSensorModalities.Radar));
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    private static bool ColumnExists(string dbPath, string table, string column)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static void ClearDb(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
