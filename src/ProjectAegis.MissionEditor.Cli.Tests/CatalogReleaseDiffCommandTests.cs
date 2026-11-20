using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogReleaseDiffCommandTests
{
    [Test]
    public void TlRelease_catalog_release_diff_returns_empty_golden_for_identical_versions()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-release-diff-same-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-cli-same";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-same");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-cli-same"],
                    createdUtcTicks: 9600);
            }

            using var writer = new StringWriter();
            Assert.That(CatalogReleaseDiffCommand.Run(dbPath, unifiedVersion, unifiedVersion, writer), Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("catalog_release_diff"));
            Assert.That(root.GetProperty("isEmpty").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("diffCount").GetInt32(), Is.EqualTo(0));
            Assert.That(root.GetProperty("canonicalLines").ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(root.GetProperty("canonicalLines").GetArrayLength(), Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogImport_catalog_release_diff_emits_sorted_delta_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-release-diff-delta-{Guid.NewGuid():N}.db");
        const string fromVersion = "unified-corpus-TL-0-cli-from";
        const string toVersion = "unified-corpus-TL-0-cli-to";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-from");
            SeedDomainRelease(dbPath, "nightly-platform-s32-07-cli-from");
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-to", maxRecords: 12);
            SeedDomainRelease(dbPath, "nightly-weapon-s32-07-cli-to");

            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    fromVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-cli-from", "nightly-platform-s32-07-cli-from"],
                    createdUtcTicks: 9610);

                store.RecordUnifiedRelease(
                    toVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-07-cli-to", "nightly-weapon-s32-07-cli-to"],
                    createdUtcTicks: 9620);
            }

            using var writer = new StringWriter();
            Assert.That(CatalogReleaseDiffCommand.Run(dbPath, fromVersion, toVersion, writer), Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("isEmpty").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("diffCount").GetInt32(), Is.EqualTo(3));

            var kinds = root.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("kind").GetString() ?? string.Empty)
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();
            Assert.That(kinds, Is.EqualTo(["Added", "Changed", "Removed"]));

            var canonical = root.GetProperty("canonicalLines")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToArray();
            Assert.That(canonical.Length, Is.EqualTo(3));
            Assert.That(canonical[0], Does.StartWith("Added\tweapon\t"));
            Assert.That(canonical[1], Does.StartWith("Changed\tsensor\t"));
            Assert.That(canonical[2], Does.StartWith("Removed\tplatform\t"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void SeedDomainRelease(
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

        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001));
        Assert.That(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "release-diff-cli-test").Committed, Is.True);
        CatalogSnapshotBinder.BindAfterApprove(
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