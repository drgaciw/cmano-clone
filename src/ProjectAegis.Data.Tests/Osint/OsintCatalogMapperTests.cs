using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Osint;

[Runner.CatalogSqliteCollection]
public sealed class OsintCatalogMapperTests
{
    [TestCase("09", 7, 7, "branch:doc-09", "osint:09")]
    [TestCase("10", 4, 4, "branch:doc-10", "osint:10")]
    [TestCase("9", 6, 6, "branch:doc-09", "osint:09")]
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

        Assert.That(binding.TrlLevel, Is.EqualTo(expectedTrl));
        Assert.That(binding.ImportBatchId, Is.EqualTo(expectedBranch));
        Assert.That(binding.SourceFactId, Is.EqualTo(expectedSourceFactId));
        Assert.That(binding.ReviewState, Is.EqualTo(CatalogReviewStates.Provisional));
    }

    [Test]
    public void ResolveTrlLevel_clamps_out_of_range_values()
    {
        Assert.That(OsintCatalogMapper.ResolveTrlLevel(0), Is.EqualTo(1));
        Assert.That(OsintCatalogMapper.ResolveTrlLevel(12), Is.EqualTo(9));
    }

    [Test]
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

        Assert.That(            second.Select(b => (b.PlatformId, b.SensorId)), Is.EqualTo(            first.Select(b => (b.PlatformId, b.SensorId))));
        Assert.That(            first.Select(b => b.SensorId).ToArray(), Is.EqualTo(            new[] { "osint-a-sensor", "osint-m-sensor", "osint-z-sensor" }));
    }

    [Test]
    public void Mapper_exposes_only_pure_binding_projection_without_write_gate_types()
    {
        var methods = typeof(OsintCatalogMapper)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(OsintCatalogMapper));

        foreach (var method in methods)
        {
            Assert.That(method.ReturnType.FullName ?? string.Empty, Does.Not.Contain("IWriteGate"));
            foreach (var param in method.GetParameters())
            {
                Assert.That(param.ParameterType.FullName ?? string.Empty, Does.Not.Contain("IWriteGate"));
            }
        }

        var bindings = OsintCatalogMapper.ToSensorBindings(
        [
            new OsintDiscoveryRecord("pure-map", "https://example.org/pure", "snippet", 0.72, "09", 6),
        ]);

        Assert.That(bindings, Has.Exactly(1).Items);
        Assert.That(bindings[0], Is.TypeOf<CatalogSensorBinding>());
    }

    [Test]
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

        Assert.That(quarantined, Has.Exactly(1).Items);
        Assert.That(quarantined[0].RejectionReason, Is.EqualTo("trl_below_minimum"));
    }

    [Test]
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
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetString(0), Is.EqualTo("osint-near-future-radar"));
            Assert.That(reader.GetInt32(1), Is.EqualTo(7));
            Assert.That(reader.GetString(2), Is.EqualTo("branch:doc-09"));
            Assert.That(reader.GetString(3), Is.EqualTo("osint:09"));

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetString(0), Is.EqualTo("osint-speculative-rail"));
            Assert.That(reader.GetInt32(1), Is.EqualTo(5));
            Assert.That(reader.GetString(2), Is.EqualTo("branch:doc-10"));
            Assert.That(reader.GetString(3), Is.EqualTo("osint:10"));
            Assert.That(reader.Read(), Is.False);
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