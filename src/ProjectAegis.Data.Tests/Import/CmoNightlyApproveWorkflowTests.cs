using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S29-03 / S30-04 curated nightly propose→approve→RecordRelease regression (off-CI path).</summary>
[Runner.CatalogSqliteCollection]
public sealed class CmoNightlyApproveWorkflowTests
{
    [Test]
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

            Assert.That(proposed.ParsedCount, Is.EqualTo(12));
            Assert.That(proposed.Batches, Is.Not.Empty);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29032)))
            {
                foreach (var batch in proposed.Batches)
                {
                    Assert.That(gate.ApproveBatch(batch.BatchId, "human", "nightly-curator").Committed, Is.True);
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

            Assert.That(bind.ContentHashSha256, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(bind.ReleaseVersion, Is.EqualTo("nightly-platform-s29-03"));

            using var store = new DbSnapshotStore(dbPath);
            Assert.That(store.TryGetContentHash(bind.SnapshotId, out var storedHash), Is.True);
            Assert.That(storedHash, Is.EqualTo(bind.ContentHashSha256));

            var releases = store.GetSortedReleases();
            Assert.That(releases.Any(r => r.ReleaseVersion == "nightly-platform-s29-03"), Is.True);
            Assert.That(releases.Any(r => r.Notes.Contains(platformBatchId, StringComparison.Ordinal)), Is.True);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            foreach (var platform in expectedPlatforms)
            {
                Assert.That(CountPlatformRows(connection, platform.PlatformId), Is.EqualTo(1));
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void ProposePlatformsFromMarkdown_default_clock_assigns_unique_batch_ids_per_chunk()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-batch-id-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                mapBalticPlatformIds: false,
                maxRecords: 12,
                chunkSize: 5);

            var platformBatchIds = proposed.Batches
                .Where(batch => batch.BatchId.StartsWith("batch-platform-", StringComparison.Ordinal))
                .Select(batch => batch.BatchId)
                .ToArray();

            Assert.That(platformBatchIds.Length >= 2, Is.True);
            Assert.That(platformBatchIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(platformBatchIds.Length));
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

            Assert.That(proposed.ParsedCount, Is.EqualTo(12));
            var platformBatches = proposed.Batches
                .Where(batch => batch.BatchId.StartsWith("batch-platform-", StringComparison.Ordinal))
                .ToArray();
            Assert.That(platformBatches.Length >= 2, Is.True);
            foreach (var batch in platformBatches)
{
Assert.That(batch.RecordCount > 0, Is.True);
}

            var batchIds = proposed.Batches
                .Select(batch => batch.BatchId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(batchIds.Length >= 2, Is.True);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29042)))
            {
                foreach (var batchId in batchIds)
                {
                    var decision = gate.ApproveBatch(batchId, "human", "nightly-curator");
                    Assert.That(decision.Committed, Is.True);
                }
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            foreach (var platform in expectedPlatforms)
            {
                Assert.That(CountPlatformRows(connection, platform.PlatformId), Is.EqualTo(1));
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

            Assert.That(approvedStates.Count >= 2, Is.True);
            foreach (var state in approvedStates)
{
Assert.That(state, Is.EqualTo("approved"));
}
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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

            Assert.That(proposed.ParsedCount, Is.EqualTo(10));
            var batchId = proposed.Batches[0].BatchId;

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(29052)))
            {
                Assert.That(gate.ApproveBatch(batchId, "human", "nightly-curator").Committed, Is.True);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(29053),
                releaseVersion: "nightly-sensor-s29-03");

            Assert.That(bind.ContentHashSha256, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(bind.SensorRowCount >= 10, Is.True);

            using var store = new DbSnapshotStore(dbPath);
            var releases = store.GetSortedReleases();
            Assert.That(releases.Any(r => r.ReleaseVersion == "nightly-sensor-s29-03"), Is.True);
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