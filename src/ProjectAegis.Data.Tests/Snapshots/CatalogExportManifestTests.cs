using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class CatalogExportManifestTests
{
    [Fact]
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
        Assert.NotNull(meta);
        Assert.Equal("db-20261001-tl0-001", MetaValue(meta!, "DbVersion"));
        Assert.Equal(CatalogTlTier.Tl2, MetaValue(meta!, "TlTier"));
        Assert.Equal(CatalogTlTier.CatalogSchemaVersion, MetaValue(meta!, "CatalogSchemaVersion"));
        Assert.Equal("abc123", MetaValue(meta!, "ContentHash"));
        Assert.Equal(CatalogTlTier.ExportManifestSchemaVersion, MetaValue(meta!, "ExportSchemaVersion"));
    }

    [Fact]
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
                Assert.True(gate.ApproveBatch(batchId, "human", "tl-manifest-test").Committed);
            }

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                batchId,
                new FixedCatalogClock(5002),
                tlTier: CatalogTlTier.Tl3);

            using var store = new DbSnapshotStore(dbPath);
            Assert.True(store.TryGetBranch(bind.SnapshotId, out var branch));
            Assert.Equal(CatalogTlTier.Tl3, branch);
            Assert.Equal(CatalogTlTier.Tl3, bind.TlTier);

            var manifest = CatalogExportManifest.Resolve(dbPath, bind.SnapshotId, bind.ReleaseVersion);
            Assert.Equal(bind.ReleaseVersion, manifest.DbVersion);
            Assert.Equal(CatalogTlTier.Tl3, manifest.TlTier);
            Assert.Equal(bind.ContentHashSha256, manifest.ContentHash);
            Assert.Equal(CatalogTlTier.CatalogSchemaVersion, manifest.SchemaVersion);
            Assert.Equal(CatalogTlTier.ExportManifestSchemaVersion, manifest.ExportSchemaVersion);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
            Assert.NotEmpty(approved);
            Assert.Empty(quarantined);
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