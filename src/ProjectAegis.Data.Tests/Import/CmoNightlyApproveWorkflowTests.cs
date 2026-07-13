using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S29-03 / S30-04 curated nightly propose→approve→RecordRelease regression (off-CI path).</summary>
[Collection("CatalogSqlite")]
public sealed class CmoNightlyApproveWorkflowTests
{
    [Fact]
    public void Nightly_platform_slice_propose_approve_records_pinned_snapshot_hash()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var expectedPlatforms = ExpectedPlatformSlice(platformPath, maxRecords: 12);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s29-nightly-approve-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                mapBalticPlatformIds: false,
                maxRecords: 12,
                chunkSize: 500,
                clock: new FixedCatalogClock(29031));

            Assert.Equal(12, proposed.ParsedCount);
            Assert.NotEmpty(proposed.Batches);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29032)))
            {
                foreach (var batch in proposed.Batches)
                {
                    Assert.True(gate.ApproveBatch(batch.BatchId, "human", "nightly-curator").Committed);
                }
            }

            var platformBatchId = proposed.Batches
                .First(batch => batch.BatchId.StartsWith("batch-platform-", StringComparison.Ordinal))
                .BatchId;

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                platformBatchId,
                new FixedCatalogClock(29033),
                releaseVersion: "nightly-platform-s29-03");

            Assert.Matches("^[a-f0-9]{64}$", bind.ContentHashSha256);
            Assert.Equal("nightly-platform-s29-03", bind.ReleaseVersion);

            using var store = new DbSnapshotStore(dbPath);
            Assert.True(store.TryGetContentHash(bind.SnapshotId, out var storedHash));
            Assert.Equal(bind.ContentHashSha256, storedHash);

            var releases = store.GetSortedReleases();
            Assert.Contains(releases, r => r.ReleaseVersion == "nightly-platform-s29-03");
            Assert.Contains(releases, r => r.Notes.Contains(platformBatchId, StringComparison.Ordinal));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            foreach (var platform in expectedPlatforms)
            {
                Assert.Equal(1, CountPlatformRows(connection, platform.PlatformId));
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_multi_batch_platform_approve_commits_all_chunks_via_WriteGate()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var expectedPlatforms = ExpectedPlatformSlice(platformPath, maxRecords: 12);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s29-nightly-multi-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                mapBalticPlatformIds: false,
                maxRecords: 12,
                chunkSize: 5,
                clock: new FixedCatalogClock(29041));

            Assert.Equal(12, proposed.ParsedCount);
            var platformBatches = proposed.Batches
                .Where(batch => batch.BatchId.StartsWith("batch-platform-", StringComparison.Ordinal))
                .ToArray();
            Assert.True(platformBatches.Length >= 2);
            Assert.All(platformBatches, batch => Assert.True(batch.RecordCount > 0));

            var batchIds = proposed.Batches
                .Select(batch => batch.BatchId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.True(batchIds.Length >= 2);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29042)))
            {
                foreach (var batchId in batchIds)
                {
                    var decision = gate.ApproveBatch(batchId, "human", "nightly-curator");
                    Assert.True(decision.Committed);
                }
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            foreach (var platform in expectedPlatforms)
            {
                Assert.Equal(1, CountPlatformRows(connection, platform.PlatformId));
            }

            using var batchCmd = connection.CreateCommand();
            batchCmd.CommandText =
                """
                SELECT approval_state FROM catalog_staging_batch
                WHERE batch_id LIKE 'batch-platform-%'
                ORDER BY batch_id ASC
                """;
            using var reader = batchCmd.ExecuteReader();
            var approvedStates = new List<string>();
            while (reader.Read())
            {
                approvedStates.Add(reader.GetString(0));
            }

            Assert.True(approvedStates.Count >= 2);
            Assert.All(approvedStates, state => Assert.Equal("approved", state));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_sensor_slice_propose_approve_records_release_row()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s29-nightly-sensor-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 10,
                chunkSize: 500,
                clock: new FixedCatalogClock(29051));

            Assert.Equal(10, proposed.ParsedCount);
            var batchId = proposed.Batches[0].BatchId;

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29052)))
            {
                Assert.True(gate.ApproveBatch(batchId, "human", "nightly-curator").Committed);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(29053),
                releaseVersion: "nightly-sensor-s29-03");

            Assert.Matches("^[a-f0-9]{64}$", bind.ContentHashSha256);
            Assert.True(bind.SensorRowCount >= 10);

            using var store = new DbSnapshotStore(dbPath);
            var releases = store.GetSortedReleases();
            Assert.Contains(releases, r => r.ReleaseVersion == "nightly-sensor-s29-03");
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static IReadOnlyList<CatalogPlatformBinding> ExpectedPlatformSlice(string platformPath, int maxRecords)
    {
        var platforms = CmoMarkdownImporter.ReadPlatformBindings(platformPath);
        return CatalogSortKeyComparer.SortPlatforms(platforms).Take(maxRecords).ToArray();
    }

    private static int CountPlatformRows(SqliteConnection connection, string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM platform WHERE platform_id = $platformId";
        cmd.Parameters.AddWithValue("$platformId", platformId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
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