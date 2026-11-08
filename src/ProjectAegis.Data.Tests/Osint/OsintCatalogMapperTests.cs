using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Osint;

[Collection("CatalogSqlite")]
public sealed class OsintCatalogMapperTests
{
    [Theory]
    [InlineData("09", 7, 7, "branch:doc-09", "osint:09")]
    [InlineData("10", 4, 4, "branch:doc-10", "osint:10")]
    [InlineData("9", 6, 6, "branch:doc-09", "osint:09")]
    public void ToSensorBinding_maps_proposed_trl_and_target_doc_to_staged_metadata(
        string targetDoc,
        int proposedTrl,
        int expectedTrl,
        string expectedBranch,
        string expectedSourceFactId)
    {
        var record = new OsintDiscoveryRecord(
            "hypersonic-glide",
            "https://example.org/hypersonic",
            "observed boost-glide",
            0.81,
            targetDoc,
            proposedTrl);

        var binding = OsintCatalogMapper.ToSensorBinding(record, "osint-platform");

        Assert.Equal(expectedTrl, binding.TrlLevel);
        Assert.Equal(expectedBranch, binding.ImportBatchId);
        Assert.Equal(expectedSourceFactId, binding.SourceFactId);
        Assert.Equal(CatalogReviewStates.Provisional, binding.ReviewState);
    }

    [Fact]
    public void ResolveTrlLevel_clamps_out_of_range_values()
    {
        Assert.Equal(1, OsintCatalogMapper.ResolveTrlLevel(0));
        Assert.Equal(9, OsintCatalogMapper.ResolveTrlLevel(12));
    }

    [Fact]
    public void ToSensorBindings_orders_deterministically_by_platform_then_sensor()
    {
        var records = new[]
        {
            new OsintDiscoveryRecord("z-sensor", "https://z.example", "z", 0.7, "10", 5),
            new OsintDiscoveryRecord("a-sensor", "https://a.example", "a", 0.8, "09", 6),
            new OsintDiscoveryRecord("m-sensor", "https://m.example", "m", 0.75, "09", 7),
        };

        var first = OsintCatalogMapper.ToSensorBindings(records, "osint-platform");
        var second = OsintCatalogMapper.ToSensorBindings(records.Reverse(), "osint-platform");

        Assert.Equal(
            first.Select(b => (b.PlatformId, b.SensorId)),
            second.Select(b => (b.PlatformId, b.SensorId)));
        Assert.Equal(
            new[] { "osint-a-sensor", "osint-m-sensor", "osint-z-sensor" },
            first.Select(b => b.SensorId).ToArray());
    }

    [Fact]
    public void Mapper_exposes_only_pure_binding_projection_without_write_gate_types()
    {
        var methods = typeof(OsintCatalogMapper)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(OsintCatalogMapper));

        foreach (var method in methods)
        {
            Assert.DoesNotContain("IWriteGate", method.ReturnType.FullName ?? string.Empty);
            foreach (var param in method.GetParameters())
            {
                Assert.DoesNotContain("IWriteGate", param.ParameterType.FullName ?? string.Empty);
            }
        }

        var bindings = OsintCatalogMapper.ToSensorBindings(
        [
            new OsintDiscoveryRecord("pure-map", "https://example.org/pure", "snippet", 0.72, "09", 6),
        ]);

        Assert.Single(bindings);
        Assert.IsType<CatalogSensorBinding>(bindings[0]);
    }

    [Fact]
    public void Low_trl_binding_quarantined_by_import_gate_never_promotes()
    {
        var record = new OsintDiscoveryRecord(
            "speculative-low-trl",
            "https://example.org/low",
            "below trl gate",
            0.7,
            "10",
            2);

        var binding = OsintCatalogMapper.ToSensorBinding(record);

        var (_, quarantined) = CatalogImportGate.PartitionForImport([binding], requireApproved: false);

        Assert.Single(quarantined);
        Assert.Equal("trl_below_minimum", quarantined[0].RejectionReason);
    }

    [Fact]
    public void Propose_via_write_gate_preserves_tl_routing_on_staged_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-mapper-{Guid.NewGuid():N}.db");

        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var records = new[]
            {
                new OsintDiscoveryRecord("near-future-radar", "https://example.org/nf", "nf", 0.78, "09", 7),
                new OsintDiscoveryRecord("speculative-rail", "https://example.org/sp", "sp", 0.66, "10", 5),
            };
            var bindings = OsintCatalogMapper.ToSensorBindings(records, "osint-s22");

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9001));
            var batchId = gate.ProposeSensorBatch(bindings, "osint-digest", "s22-07-test");

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT sensor_id, trl_level, import_batch_id, source_fact_id
                FROM catalog_staging_sensor
                WHERE batch_id = $batch
                ORDER BY sensor_id ASC
                """;
            cmd.Parameters.AddWithValue("$batch", batchId);

            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("osint-near-future-radar", reader.GetString(0));
            Assert.Equal(7, reader.GetInt32(1));
            Assert.Equal("branch:doc-09", reader.GetString(2));
            Assert.Equal("osint:09", reader.GetString(3));

            Assert.True(reader.Read());
            Assert.Equal("osint-speculative-rail", reader.GetString(0));
            Assert.Equal(5, reader.GetInt32(1));
            Assert.Equal("branch:doc-10", reader.GetString(2));
            Assert.Equal("osint:10", reader.GetString(3));
            Assert.False(reader.Read());
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
}