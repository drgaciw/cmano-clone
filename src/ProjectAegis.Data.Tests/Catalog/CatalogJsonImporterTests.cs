using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CatalogJsonImporterTests
{
    [Fact]
    public void ImportToSqlite_reads_json_drop_deterministically()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-json-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using (var reader = new SqliteCatalogReader(dbPath, "p0-json-test"))
            {
                Assert.Equal(2, reader.GetSortedSensorBindings().Count);
                Assert.True(reader.TryGetBasePd("u1", "radar-2", out var pd));
                Assert.Equal(0.75, pd);
            }
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