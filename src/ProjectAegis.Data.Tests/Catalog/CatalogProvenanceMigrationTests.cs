using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[Runner.CatalogSqliteCollection]
public sealed class CatalogProvenanceMigrationTests
{
    [Test]
    public void Sqlite_reader_loads_provenance_columns()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-prov-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "p0-prov-test");
            var binding = reader.GetSortedSensorBindings()[0];
            Assert.That(binding.ImportBatchId, Is.EqualTo("baltic-patrol-2026-06"));
            Assert.That(binding.SourceFile, Is.EqualTo("sensors_baltic.json"));
            Assert.That(binding.ReviewState, Is.EqualTo(CatalogReviewStates.Approved));
            Assert.That(binding.TrlLevel, Is.EqualTo(9));
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