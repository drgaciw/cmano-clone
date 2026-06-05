using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownImportSmokeTests
{
    [Fact]
    public void Mini_fixture_parses_at_least_ten_sensor_rows_deterministically()
    {
        var path = CmoMarkdownImporter.ResolveMiniFixturePath();
        var first = CmoMarkdownImporter.ReadSensorBindings(path);
        var second = CmoMarkdownImporter.ReadSensorBindings(path);

        Assert.True(first.Count >= 10);
        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].PlatformId, second[i].PlatformId);
            Assert.Equal(first[i].SensorId, second[i].SensorId);
            Assert.Equal(first[i].BasePd, second[i].BasePd);
        }

        var radar = Assert.Single(first, b => b.SensorId == "cmo-sensor-1001");
        Assert.Equal("cmano-db:sensor/1001", radar.SourceFactId);
        Assert.Equal("test-radar-an-spy-1", radar.PlatformId);
    }

    [Fact]
    public void Markdown_import_propose_approve_commits_via_write_gate()
    {
        var markdownPath = CmoMarkdownImporter.ResolveMiniFixturePath();
        var bindings = CmoMarkdownImporter.ReadSensorBindings(markdownPath);
        Assert.True(bindings.Count >= 10);

        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-md-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9001)))
            {
                var batchId = gate.ProposeSensorBatch(bindings, "agent", "cmo-markdown-importer");
                var decision = gate.ApproveBatch(batchId, "human", "data-5-smoke");
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "cmo-md-smoke");
            var committed = reader.GetSortedSensorBindings()
                .Where(b => b.SensorId.StartsWith("cmo-sensor-", StringComparison.Ordinal))
                .ToArray();
            Assert.True(committed.Length >= 10);
            Assert.True(reader.TryGetBasePd("test-radar-an-spy-1", "cmo-sensor-1001", out var basePd));
            Assert.True(basePd > 0 && basePd <= 1.0);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM catalog_change_log WHERE batch_id LIKE 'batch-%'";
            var logCount = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(logCount >= 10);
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
    public void Reference_sensor_markdown_subset_parses_without_node_toolchain()
    {
        var path = CmoMarkdownImporter.ResolveReferenceSensorMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var bindings = CmoMarkdownImporter.ReadSensorBindings(path, maxRecords: 10);
        Assert.True(bindings.Count >= 10);
        Assert.All(bindings, b => Assert.Matches(@"^cmo-sensor-\d+$", b.SensorId));
    }
}