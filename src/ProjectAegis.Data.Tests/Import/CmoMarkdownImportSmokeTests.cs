using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownImportSmokeTests
{
    [Test]
    public void Mini_fixture_parses_at_least_ten_sensor_rows_deterministically()
    {
        var path = CmoMarkdownImporter.ResolveMiniFixturePath();
        var first = CmoMarkdownImporter.ReadSensorBindings(path);
        var second = CmoMarkdownImporter.ReadSensorBindings(path);

        Assert.That(first.Count >= 10, Is.True);
        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.That(second[i].PlatformId, Is.EqualTo(first[i].PlatformId));
            Assert.That(second[i].SensorId, Is.EqualTo(first[i].SensorId));
            Assert.That(second[i].BasePd, Is.EqualTo(first[i].BasePd));
        }

        var radar = ProjectAegis.Data.Tests.AssertEx.Single(first, b => b.SensorId == "cmo-sensor-1001");
        Assert.That(radar.SourceFactId, Is.EqualTo("cmano-db:sensor/1001"));
        Assert.That(radar.PlatformId, Is.EqualTo("test-radar-an-spy-1"));
    }

    [Test]
    public void Markdown_import_propose_approve_commits_via_write_gate()
    {
        var markdownPath = CmoMarkdownImporter.ResolveMiniFixturePath();
        var bindings = CmoMarkdownImporter.ReadSensorBindings(markdownPath);
        Assert.That(bindings.Count >= 10, Is.True);

        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-md-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9001)))
            {
                var batchId = gate.ProposeSensorBatch(bindings, "agent", "cmo-markdown-importer");
                var decision = gate.ApproveBatch(batchId, "human", "data-5-smoke");
                Assert.That(decision.Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "cmo-md-smoke");
            var committed = reader.GetSortedSensorBindings()
                .Where(b => b.SensorId.StartsWith("cmo-sensor-", StringComparison.Ordinal))
                .ToArray();
            Assert.That(committed.Length >= 10, Is.True);
            Assert.That(reader.TryGetBasePd("test-radar-an-spy-1", "cmo-sensor-1001", out var basePd), Is.True);
            Assert.That(basePd > 0 && basePd <= 1.0, Is.True);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM catalog_change_log WHERE batch_id LIKE 'batch-%'";
            var logCount = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(logCount >= 10, Is.True);
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
    public void Reference_sensor_markdown_subset_parses_without_node_toolchain()
    {
        var path = CmoMarkdownImporter.ResolveReferenceSensorMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var bindings = CmoMarkdownImporter.ReadSensorBindings(path, maxRecords: 10);
        Assert.That(bindings.Count >= 10, Is.True);
        foreach (var b in bindings)
{
Assert.That(b.SensorId, Does.Match(@"^cmo-sensor-\d+$"));
}
    }
}