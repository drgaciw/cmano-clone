using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogReleaseDiffCommandTests
{
    [Fact]
    public void TlRelease_catalog_release_diff_returns_empty_golden_for_identical_versions()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-release-diff-same-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-cli-same";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-same", batchSuffix: "sensor-cli-same");
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
            Assert.Equal(
                0,
                CatalogReleaseDiffCommand.Run(dbPath, unifiedVersion, unifiedVersion, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("catalog_release_diff", root.GetProperty("verb").GetString());
            Assert.True(root.GetProperty("isEmpty").GetBoolean());
            Assert.Equal(0, root.GetProperty("diffCount").GetInt32());
            Assert.Equal(JsonValueKind.Array, root.GetProperty("canonicalLines").ValueKind);
            Assert.Equal(0, root.GetProperty("canonicalLines").GetArrayLength());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogImport_catalog_release_diff_emits_sorted_delta_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-release-diff-delta-{Guid.NewGuid():N}.db");
        const string fromVersion = "unified-corpus-TL-0-cli-from";
        const string toVersion = "unified-corpus-TL-0-cli-to";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-from", batchSuffix: "sensor-cli-from");
            SeedDomainRelease(dbPath, "nightly-platform-s32-07-cli-from", batchSuffix: "platform-cli-from");
            SeedDomainRelease(dbPath, "nightly-sensor-s32-07-cli-to", batchSuffix: "sensor-cli-to", maxRecords: 12);
            SeedDomainRelease(dbPath, "nightly-weapon-s32-07-cli-to", batchSuffix: "weapon-cli-to");

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
            Assert.Equal(0, CatalogReleaseDiffCommand.Run(dbPath, fromVersion, toVersion, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.False(root.GetProperty("isEmpty").GetBoolean());
            Assert.Equal(3, root.GetProperty("diffCount").GetInt32());

            var kinds = root.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("kind").GetString() ?? string.Empty)
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(["Added", "Changed", "Removed"], kinds);

            var canonical = root.GetProperty("canonicalLines")
                .EnumerateArray()
                .Select(e => e.GetString())
                .ToArray();
            Assert.Equal(3, canonical.Length);
            Assert.StartsWith("Added\tweapon\t", canonical[0], StringComparison.Ordinal);
            Assert.StartsWith("Changed\tsensor\t", canonical[1], StringComparison.Ordinal);
            Assert.StartsWith("Removed\tplatform\t", canonical[2], StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void SeedDomainRelease(
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

        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001));
        Assert.True(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "release-diff-cli-test").Committed);
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