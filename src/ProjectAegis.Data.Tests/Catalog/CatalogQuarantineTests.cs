using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CatalogQuarantineTests
{
    [Fact]
    public void ImportToSqlite_writes_rejected_rows_to_quarantine_table()
    {
        var jsonPath = Path.Combine(
            CatalogBulkImporter.ResolveCatalogImportDirectory(),
            "sensor_quarantine_sample.json");
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-quarantine-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sensor_quarantine WHERE rejection_reason LIKE 'review_state_%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(1, count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}