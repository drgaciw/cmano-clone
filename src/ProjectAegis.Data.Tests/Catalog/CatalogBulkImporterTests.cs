using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[Runner.CatalogSqliteCollection]
public sealed class CatalogBulkImporterTests
{
    [Test]
    public void ImportDirectory_merges_sorted_json_drops()
    {
        var dir = CatalogBulkImporter.ResolveCatalogImportDirectory();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-bulk-{Guid.NewGuid():N}.db");
        try
        {
            var fileCount = CatalogBulkImporter.ImportDirectory(dir, dbPath);
            Assert.That(fileCount >= 1, Is.True);
            using var reader = new SqliteCatalogReader(dbPath, "p0-bulk-test");
            Assert.That(reader.TryGetBasePd("u1", "radar-2", out var pd), Is.True);
            Assert.That(pd, Is.EqualTo(0.75));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}