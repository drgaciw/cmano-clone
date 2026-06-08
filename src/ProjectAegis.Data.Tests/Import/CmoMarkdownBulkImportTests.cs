using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownBulkImportTests
{
    [Fact]
    public void ChunkBindings_with_501_rows_produces_two_batches_at_chunk_size_500()
    {
        var rows = Enumerable.Range(0, 501)
            .Select(i => new CatalogSensorBinding($"platform-{i:D4}", $"sensor-{i:D4}", 0.5))
            .ToArray();

        var chunks = CmoMarkdownImportProposer.ChunkBindings(rows, chunkSize: 500);

        Assert.Equal(2, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Single(chunks[1]);
    }

    [Fact]
    public void ProposeFromMarkdown_with_501_sensors_produces_two_propose_batches()
    {
        var markdownPath = WriteBulkFixture(sensorCount: 501, quarantineSensorIndex: -1);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-p2-bulk-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdownPath,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(100));

            Assert.Equal(501, result.ApprovedCount);
            Assert.Equal(0, result.QuarantinedCount);
            Assert.Equal(2, result.Batches.Count);
            Assert.Equal(500, result.Batches[0].RecordCount);
            Assert.Equal(1, result.Batches[1].RecordCount);
            Assert.Empty(result.QuarantineReport);
        }
        finally
        {
            Cleanup(dbPath, markdownPath);
        }
    }

    [Fact]
    public void ProposeFromMarkdown_quarantine_rows_written_and_report_populated()
    {
        var markdownPath = WriteBulkFixture(sensorCount: 3, quarantineSensorIndex: 1);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-p2-quarantine-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdownPath,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(101));

            Assert.Equal(2, result.ApprovedCount);
            Assert.Equal(1, result.QuarantinedCount);
            Assert.Single(result.QuarantineReport);
            Assert.Equal("confidence_below_minimum", result.QuarantineReport[0].Reason);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sensor_quarantine";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(1, count);
        }
        finally
        {
            Cleanup(dbPath, markdownPath);
        }
    }

    private static string WriteBulkFixture(int sensorCount, int quarantineSensorIndex)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bulk-md-{Guid.NewGuid():N}.md");
        var sb = new StringBuilder("# Bulk sensor fixture\n\n");
        for (var i = 0; i < sensorCount; i++)
        {
            var sensorId = 40_000 + i;
            sb.AppendLine(CultureInfo.InvariantCulture, $"### Test Radar Bulk {i}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"<sub>[/sensor/{sensorId}/](https://cmano-db.com/sensor/{sensorId}/)</sub>");
            sb.AppendLine();
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Type | Radar |");
            if (i == quarantineSensorIndex)
            {
                sb.AppendLine("| Confidence | 0.1 |");
            }

            sb.AppendLine("| Range Max | 100 nm |");
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static void Cleanup(string dbPath, string markdownPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        if (File.Exists(markdownPath))
        {
            File.Delete(markdownPath);
        }
    }
}