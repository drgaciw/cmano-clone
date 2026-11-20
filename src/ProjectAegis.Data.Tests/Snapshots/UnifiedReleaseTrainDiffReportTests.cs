using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Snapshots;

[Runner.CatalogSqliteCollection]
public sealed class UnifiedReleaseTrainDiffReportTests
{
    [Test]
    public void TlRelease_diff_is_empty_for_identical_release_versions()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-diff-same-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-diff-same";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-same");
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
            Assert.That(report.IsEmpty, Is.True);
            Assert.That(report.ToSortedCanonicalLines(), Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
            var sensorFrom = SeedDomainRelease(dbPath, sensorFromVersion);
            var platformFrom = SeedDomainRelease(dbPath, platformFromVersion);
            var sensorTo = SeedDomainRelease(dbPath, sensorToVersion, maxRecords: 12);
            var weaponTo = SeedDomainRelease(dbPath, weaponToVersion);

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
            Assert.That(report.IsEmpty, Is.False);
            Assert.That(report.Rows.Count, Is.EqualTo(3));

            var lines = report.ToSortedCanonicalLines();
            Assert.That(report.ToSortedCanonicalLines(), Is.EqualTo(lines));
            Assert.That(                lines, Is.EqualTo(                [
                    $"Added\tweapon\t\t{weaponToVersion}\t\t{CatalogValidationDefaults.BalticSnapshotId}\t\t{weaponTo.ContentHashSha256}",
                    $"Changed\tsensor\t{sensorFromVersion}\t{sensorToVersion}\t{CatalogValidationDefaults.BalticSnapshotId}\t{CatalogValidationDefaults.BalticSnapshotId}\t{sensorFrom.ContentHashSha256}\t{sensorTo.ContentHashSha256}",
                    $"Removed\tplatform\t{platformFromVersion}\t\t{CatalogValidationDefaults.BalticSnapshotId}\t\t{platformFrom.ContentHashSha256}\t",
                ]));

            Assert.That(report.Rows.Single(r => r.Domain == "weapon").Kind, Is.EqualTo(UnifiedReleaseTrainDiffKind.Added));
            Assert.That(report.Rows.Single(r => r.Domain == "platform").Kind, Is.EqualTo(UnifiedReleaseTrainDiffKind.Removed));
            Assert.That(report.Rows.Single(r => r.Domain == "sensor").Kind, Is.EqualTo(UnifiedReleaseTrainDiffKind.Changed));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogImport_reimport_empty_diff_when_semantic_content_matches()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-release-diff-reimport-{Guid.NewGuid():N}.db");
        const string firstVersion = "nightly-sensor-s32-07-reimport-a";
        const string secondVersion = "nightly-sensor-s32-07-reimport-b";

        try
        {
            var first = SeedDomainRelease(dbPath, firstVersion);
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
            Assert.That(report.IsEmpty, Is.True);
            Assert.That(report.ToSortedCanonicalLines(), Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static CatalogSnapshotBinder.BindResult SeedDomainRelease(
        string dbPath,
        string releaseVersion,
        int maxRecords = 6)
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
            dbPath,
            markdown,
            maxRecords: maxRecords,
            chunkSize: 500,
            clock: new FixedCatalogClock(8000));

        using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001)))
        {
            Assert.That(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "release-diff-test").Committed, Is.True);
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