using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogBulkImporterTests
{
    [Fact]
    public void ImportDirectory_merges_sorted_json_drops()
    {
        var dir = CatalogBulkImporter.ResolveCatalogImportDirectory();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-bulk-{Guid.NewGuid():N}.db");
        try
        {
            var fileCount = CatalogBulkImporter.ImportDirectory(dir, dbPath);
            Assert.True(fileCount >= 1);
            using var reader = new SqliteCatalogReader(dbPath, "p0-bulk-test");
            Assert.True(reader.TryGetBasePd("u1", "radar-2", out var pd));
            Assert.Equal(0.75, pd);
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