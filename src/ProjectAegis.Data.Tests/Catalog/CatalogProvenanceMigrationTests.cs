using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogProvenanceMigrationTests
{
    [Fact]
    public void Sqlite_reader_loads_provenance_columns()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-prov-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "p0-prov-test");
            var binding = reader.GetSortedSensorBindings()[0];
            Assert.Equal("baltic-patrol-2026-06", binding.ImportBatchId);
            Assert.Equal("sensors_baltic.json", binding.SourceFile);
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