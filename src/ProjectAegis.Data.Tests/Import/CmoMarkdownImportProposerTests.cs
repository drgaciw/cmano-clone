using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownImportProposerTests
{
    [Test]
    public void ChunkBindings_splits_sorted_rows_deterministically()
    {
        var rows = Enumerable.Range(0, 5)
            .Select(i => new CatalogSensorBinding($"p{i}", $"s{i}", 0.5))
            .Reverse()
            .ToArray();

        var chunks = CmoMarkdownImportProposer.ChunkBindings(rows, chunkSize: 2);

        Assert.That(chunks.Length, Is.EqualTo(3));
        Assert.That(chunks[0].Length, Is.EqualTo(2));
        Assert.That(chunks[0][0].PlatformId, Is.EqualTo("p0"));
        Assert.That(chunks[1][0].PlatformId, Is.EqualTo("p2"));
        Assert.That(chunks[2], Has.Exactly(1).Items);
    }

    [Test]
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

            Assert.That(result.ParsedCount >= 10, Is.True);
            Assert.That(result.ApprovedCount >= 10, Is.True);
            Assert.That(result.Batches, Is.Not.Empty);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(43)))
            {
                foreach (var batch in result.Batches)
                {
                    var decision = gate.ApproveBatch(batch.BatchId, "human", "p2-smoke");
                    Assert.That(decision.Committed, Is.True);
                }
            }

            using var reader = new SqliteCatalogReader(dbPath, "p2-import");
            var committed = reader.GetSortedSensorBindings()
                .Count(b => b.SensorId.StartsWith("cmo-sensor-", StringComparison.Ordinal));
            Assert.That(committed >= 10, Is.True);
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