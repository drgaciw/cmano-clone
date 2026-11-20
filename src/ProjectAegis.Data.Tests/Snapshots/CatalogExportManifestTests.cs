using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Snapshots;

[Runner.CatalogSqliteCollection]
public sealed class CatalogExportManifestTests
{
    [Test]
    public void Export_meta_sheet_carries_tl_export_manifest_fields()
    {
        var exporter = new PlatformWorkbookExporter();
        var manifest = new CatalogExportManifest(
            DbVersion: "db-20261001-tl0-001",
            TlTier: CatalogTlTier.Tl2,
            SchemaVersion: CatalogTlTier.CatalogSchemaVersion,
            ContentHash: "abc123",
            ExportSchemaVersion: CatalogTlTier.ExportManifestSchemaVersion);

        var workbook = exporter.Export(
            PlatformCatalogExportData.Empty,
            "snap-test",
            new FixedCatalogClock(42),
            manifest);

        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.That(meta, Is.Not.Null);
        Assert.That(MetaValue(meta!, "DbVersion"), Is.EqualTo("db-20261001-tl0-001"));
        Assert.That(MetaValue(meta!, "TlTier"), Is.EqualTo(CatalogTlTier.Tl2));
        Assert.That(MetaValue(meta!, "CatalogSchemaVersion"), Is.EqualTo(CatalogTlTier.CatalogSchemaVersion));
        Assert.That(MetaValue(meta!, "ContentHash"), Is.EqualTo("abc123"));
        Assert.That(MetaValue(meta!, "ExportSchemaVersion"), Is.EqualTo(CatalogTlTier.ExportManifestSchemaVersion));
    }

    [Test]
    public void BindAfterApprove_persists_branch_and_manifest_resolves_tl_tier()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-tl-manifest-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();

        try
        {
            var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 10,
                chunkSize: 500,
                clock: new FixedCatalogClock(5000));
            var batchId = propose.Batches[0].BatchId;

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(5001)))
            {
                Assert.That(gate.ApproveBatch(batchId, "human", "tl-manifest-test").Committed, Is.True);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(5002),
                tlTier: CatalogTlTier.Tl3);

            using var store = new DbSnapshotStore(dbPath);
            Assert.That(store.TryGetBranch(bind.SnapshotId, out var branch), Is.True);
            Assert.That(branch, Is.EqualTo(CatalogTlTier.Tl3));
            Assert.That(bind.TlTier, Is.EqualTo(CatalogTlTier.Tl3));

            var manifest = CatalogExportManifest.Resolve(dbPath, bind.SnapshotId, bind.ReleaseVersion);
            Assert.That(manifest.DbVersion, Is.EqualTo(bind.ReleaseVersion));
            Assert.That(manifest.TlTier, Is.EqualTo(CatalogTlTier.Tl3));
            Assert.That(manifest.ContentHash, Is.EqualTo(bind.ContentHashSha256));
            Assert.That(manifest.SchemaVersion, Is.EqualTo(CatalogTlTier.CatalogSchemaVersion));
            Assert.That(manifest.ExportSchemaVersion, Is.EqualTo(CatalogTlTier.ExportManifestSchemaVersion));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogImportGate_partition_unchanged_when_branch_column_present()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-import-branch-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "import-branch-gate");
            var bindings = reader.GetSortedSensorBindings();

            var (approved, quarantined) = CatalogImportGate.PartitionForImport(bindings);
            Assert.That(approved, Is.Not.Empty);
            Assert.That(quarantined, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static string MetaValue(PlatformWorkbookSheet meta, string key)
    {
        foreach (var row in meta.Rows)
        {
            if (row.Count >= 2 && string.Equals(row[0], key, StringComparison.Ordinal))
            {
                return row[1];
            }
        }

        return string.Empty;
    }

    private static void Cleanup(string dbPath)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}