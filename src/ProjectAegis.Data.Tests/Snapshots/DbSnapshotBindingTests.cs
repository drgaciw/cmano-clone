using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class DbSnapshotBindingTests
{
    [Fact]
    public void BindAfterApprove_records_content_hash_and_release_row()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-p2-bind-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();

        try
        {
            var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 10,
                chunkSize: 500,
                clock: new FixedCatalogClock(3000));
            var batchId = propose.Batches[0].BatchId;

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(3001)))
            {
                var decision = gate.ApproveBatch(batchId, "human", "p2-bind-test");
                Assert.True(decision.Committed);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(3002));

            using var store = new DbSnapshotStore(dbPath);
            Assert.True(store.TryGetContentHash(bind.SnapshotId, out var hash));
            Assert.Equal(bind.ContentHashSha256, hash);
            Assert.Matches("^[a-f0-9]{64}$", hash);

            var releases = store.GetSortedReleases();
            Assert.Contains(releases, r => r.SnapshotId == bind.SnapshotId);
            Assert.Contains(releases, r => r.Notes.Contains(batchId, StringComparison.Ordinal));

            Assert.True(store.TryGetBranch(bind.SnapshotId, out var branch));
            Assert.Equal(CatalogTlTier.Default, branch);
            Assert.Equal(CatalogTlTier.Default, bind.TlTier);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Content_hash_stable_across_two_identical_approve_cycles()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var hashA = ApproveMiniFixtureAndGetHash(markdown);
        var hashB = ApproveMiniFixtureAndGetHash(markdown);
        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public void CatalogSnapshotHasher_is_order_independent()
    {
        var rows = new[]
        {
            new CatalogSensorBinding("b", "s2", 0.6),
            new CatalogSensorBinding("a", "s1", 0.5),
        };

        var forward = CatalogSnapshotHasher.ComputeSha256Hex(rows);
        var reverse = CatalogSnapshotHasher.ComputeSha256Hex(rows.Reverse().ToArray());
        Assert.Equal(forward, reverse);
    }

    private static string ApproveMiniFixtureAndGetHash(string markdownPath)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-p2-stable-{Guid.NewGuid():N}.db");
        try
        {
            var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdownPath,
                maxRecords: 10,
                chunkSize: 500,
                clock: new FixedCatalogClock(4000));

            var batchId = propose.Batches[0].BatchId;
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(4001)))
            {
                Assert.True(gate.ApproveBatch(batchId, "human", "stable-test").Committed);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(4002));
            return bind.ContentHashSha256;
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}