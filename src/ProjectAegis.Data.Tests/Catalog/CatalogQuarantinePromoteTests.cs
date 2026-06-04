using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CatalogQuarantinePromoteTests
{
    [Fact]
    public void PromoteApproved_moves_approved_quarantine_row_into_sensor()
    {
        var jsonPath = Path.Combine(
            CatalogBulkImporter.ResolveCatalogImportDirectory(),
            "sensor_quarantine_sample.json");
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-promote-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE sensor_quarantine SET review_state = $approved WHERE platform_id = 'u9'";
                cmd.Parameters.AddWithValue("$approved", CatalogReviewStates.Approved);
                cmd.ExecuteNonQuery();
            }

            var promoted = CatalogQuarantinePromoter.PromoteApproved(dbPath);
            Assert.Equal(1, promoted);

            using var verify = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            verify.Open();
            using var sensorCmd = verify.CreateCommand();
            sensorCmd.CommandText = "SELECT COUNT(*) FROM sensor WHERE platform_id = 'u9'";
            Assert.Equal(1, Convert.ToInt32(sensorCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));

            using var quarantineCmd = verify.CreateCommand();
            quarantineCmd.CommandText = "SELECT COUNT(*) FROM sensor_quarantine WHERE platform_id = 'u9'";
            Assert.Equal(0, Convert.ToInt32(quarantineCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
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