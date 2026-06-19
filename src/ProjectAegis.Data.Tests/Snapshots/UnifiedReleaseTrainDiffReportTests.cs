using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class UnifiedReleaseTrainDiffReportTests
{
    [Fact]
    public void TlRelease_diff_is_empty_for_identical_release_versions()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-diff-same-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-diff-same";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-same", batchSuffix: "sensor-same");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-same"],
                    createdUtcTicks: 9400);
            }

            using var diffStore = new DbSnapshotStore(dbPath);
            var report = UnifiedReleaseTrainDiffComparer.Compare(diffStore, unifiedVersion, unifiedVersion);
            Assert.True(report.IsEmpty);
            Assert.Empty(report.ToSortedCanonicalLines());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Snapshot_diff_lists_added_removed_changed_rows_in_sorted_order()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-diff-delta-{Guid.NewGuid():N}.db");
        const string fromVersion = "unified-corpus-TL-0-diff-from";
        const string toVersion = "unified-corpus-TL-0-diff-to";
        const string sensorFromVersion = "nightly-sensor-s32-07-from";
        const string sensorToVersion = "nightly-sensor-s32-07-to";
        const string platformFromVersion = "nightly-platform-s32-07-from";
        const string weaponToVersion = "nightly-weapon-s32-07-to";

        try
        {
            var sensorFrom = SeedDomainRelease(dbPath, sensorFromVersion, batchSuffix: "sensor-from");
            var platformFrom = SeedDomainRelease(dbPath, platformFromVersion, batchSuffix: "platform-from");
            var sensorTo = SeedDomainRelease(dbPath, sensorToVersion, batchSuffix: "sensor-to", maxRecords: 12);
            var weaponTo = SeedDomainRelease(dbPath, weaponToVersion, batchSuffix: "weapon-to");

            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    fromVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    [sensorFromVersion, platformFromVersion],
                    createdUtcTicks: 9410);

                store.RecordUnifiedRelease(
                    toVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    [sensorToVersion, weaponToVersion],
                    createdUtcTicks: 9420);
            }

            using var diffStore = new DbSnapshotStore(dbPath);
            var report = UnifiedReleaseTrainDiffComparer.Compare(diffStore, fromVersion, toVersion);
            Assert.False(report.IsEmpty);
            Assert.Equal(3, report.Rows.Count);

            var lines = report.ToSortedCanonicalLines();
            Assert.Equal(lines, report.ToSortedCanonicalLines());
            Assert.Equal(
                [
                    $"Added\tweapon\t\t{weaponToVersion}\t\t{CatalogValidationDefaults.BalticSnapshotId}\t\t{weaponTo.ContentHashSha256}",
                    $"Changed\tsensor\t{sensorFromVersion}\t{sensorToVersion}\t{CatalogValidationDefaults.BalticSnapshotId}\t{CatalogValidationDefaults.BalticSnapshotId}\t{sensorFrom.ContentHashSha256}\t{sensorTo.ContentHashSha256}",
                    $"Removed\tplatform\t{platformFromVersion}\t\t{CatalogValidationDefaults.BalticSnapshotId}\t\t{platformFrom.ContentHashSha256}\t",
                ],
                lines);

            Assert.Equal(UnifiedReleaseTrainDiffKind.Added, report.Rows.Single(r => r.Domain == "weapon").Kind);
            Assert.Equal(UnifiedReleaseTrainDiffKind.Removed, report.Rows.Single(r => r.Domain == "platform").Kind);
            Assert.Equal(UnifiedReleaseTrainDiffKind.Changed, report.Rows.Single(r => r.Domain == "sensor").Kind);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogImport_reimport_empty_diff_when_semantic_content_matches()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-diff-reimport-{Guid.NewGuid():N}.db");
        const string firstVersion = "nightly-sensor-s32-07-reimport-a";
        const string secondVersion = "nightly-sensor-s32-07-reimport-b";

        try
        {
            var first = SeedDomainRelease(dbPath, firstVersion, batchSuffix: "sensor-reimport-a");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordRelease(
                    secondVersion,
                    first.SnapshotId,
                    first.ContentHashSha256,
                    createdUtcTicks: 9500,
                    schemaVersion: CatalogTlTier.CatalogSchemaVersion,
                    notes: $"batch=reimport-b;contentHash={first.ContentHashSha256}",
                    branch: CatalogTlTier.Tl0);
            }

            using var diffStore = new DbSnapshotStore(dbPath);
            var report = UnifiedReleaseTrainDiffComparer.Compare(diffStore, firstVersion, secondVersion);
            Assert.True(report.IsEmpty);
            Assert.Empty(report.ToSortedCanonicalLines());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static CatalogSnapshotBinder.BindResult SeedDomainRelease(
        string dbPath,
        string releaseVersion,
        string batchSuffix,
        int maxRecords = 6)
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
            dbPath,
            markdown,
            maxRecords: maxRecords,
            chunkSize: 500,
            clock: new FixedCatalogClock(8000));
        var batchId = $"{propose.Batches[0].BatchId}-{batchSuffix}";

        using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001)))
        {
            Assert.True(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "release-diff-test").Committed);
        }

        return CatalogSnapshotBinder.BindAfterApprove(
            dbPath,
            propose.Batches[0].BatchId,
            new FixedCatalogClock(8002),
            releaseVersion: releaseVersion);
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