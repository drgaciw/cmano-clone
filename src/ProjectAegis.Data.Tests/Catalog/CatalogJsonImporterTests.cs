using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CatalogJsonImporterTests
{
    [Test]
    public void ImportToSqlite_reads_json_drop_deterministically()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-json-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using (var reader = new SqliteCatalogReader(dbPath, "p0-json-test"))
            {
                Assert.That(reader.GetSortedSensorBindings().Count, Is.EqualTo(2));
                Assert.That(reader.TryGetBasePd("u1", "radar-2", out var pd), Is.True);
                Assert.That(pd, Is.EqualTo(0.75));
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