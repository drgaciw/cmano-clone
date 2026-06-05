using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownImportProposerTests
{
    [Fact]
    public void ChunkBindings_splits_sorted_rows_deterministically()
    {
        var rows = Enumerable.Range(0, 5)
            .Select(i => new CatalogSensorBinding($"p{i}", $"s{i}", 0.5))
            .Reverse()
            .ToArray();

        var chunks = CmoMarkdownImportProposer.ChunkBindings(rows, chunkSize: 2);

        Assert.Equal(3, chunks.Length);
        Assert.Equal(2, chunks[0].Length);
        Assert.Equal("p0", chunks[0][0].PlatformId);
        Assert.Equal("p2", chunks[1][0].PlatformId);
        Assert.Single(chunks[2]);
    }

    [Fact]
    public void ProposeFromMarkdown_stages_mini_fixture_and_approve_commits()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-p2-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(42));

            Assert.True(result.ParsedCount >= 10);
            Assert.True(result.ApprovedCount >= 10);
            Assert.NotEmpty(result.Batches);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(43)))
            {
                foreach (var batch in result.Batches)
                {
                    var decision = gate.ApproveBatch(batch.BatchId, "human", "p2-smoke");
                    Assert.True(decision.Committed);
                }
            }

            using var reader = new SqliteCatalogReader(dbPath, "p2-import");
            var committed = reader.GetSortedSensorBindings()
                .Count(b => b.SensorId.StartsWith("cmo-sensor-", StringComparison.Ordinal));
            Assert.True(committed >= 10);
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