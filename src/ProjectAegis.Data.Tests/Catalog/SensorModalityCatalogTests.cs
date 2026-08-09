using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S111-02 / DRG-10: catalog-extend sensor modality (Radar default + IR/Visual fixtures).</summary>
[Collection("CatalogSqlite")]
public sealed class SensorModalityCatalogTests
{
    [Fact]
    public void Migration_016_adds_modality_column_and_skip_is_safe()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-modality-mig-{Guid.NewGuid():N}.db");
        try
        {
            using (var bootstrap = new SqliteCatalogReader(dbPath, "s111-modality-mig"))
            {
                Assert.True(ColumnExists(dbPath, "sensor", "modality"));
            }

            // Second open must skip re-ALTER (ShouldSkipMigration) without error.
            using (var reader = new SqliteCatalogReader(dbPath, "s111-modality-mig-reopen"))
            {
                Assert.True(ColumnExists(dbPath, "sensor", "modality"));
                _ = reader.GetSortedSensorBindings();
            }
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Fact]
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

            Assert.True(byId.TryGetValue("fixture-ir-1", out var ir));
            Assert.Equal(CatalogSensorModalities.Infrared, ir.Modality);
            Assert.Equal(0.80, ir.BasePd);

            Assert.True(byId.TryGetValue("fixture-visual-1", out var visual));
            Assert.Equal(CatalogSensorModalities.Visual, visual.Modality);
            Assert.Equal(0.70, visual.BasePd);

            Assert.True(byId.TryGetValue("radar-1", out var radar));
            Assert.Equal(CatalogSensorModalities.Radar, radar.Modality);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Fact]
    public void InMemory_default_modality_is_radar_and_v3_ir_tagged()
    {
        var patrol = InMemoryCatalogReader.BalticPatrolFixture();
        var radar = patrol.GetSortedSensorBindings()
            .Single(b => b.PlatformId == "u1" && b.SensorId == "radar-1");
        Assert.Equal(CatalogSensorModalities.Radar, radar.Modality);

        var v3 = InMemoryCatalogReader.BalticV3Fixture();
        var ir = v3.GetSortedSensorBindings()
            .Single(b => b.PlatformId == "ucav-blue" && b.SensorId == "internal-ir");
        Assert.Equal(CatalogSensorModalities.Infrared, ir.Modality);
    }

    [Fact]
    public void Reader_defaults_null_modality_to_radar()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-modality-null-{Guid.NewGuid():N}.db");
        try
        {
            using (var bootstrap = new SqliteCatalogReader(dbPath, "s111-null-mod"))
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
            Assert.Equal(CatalogSensorModalities.Radar, binding.Modality);
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
