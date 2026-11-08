using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>DRG-416: radar ECCM attributes survive JSON -> SQLite -> reader unchanged.</summary>
public sealed class CatalogRadarEccmSqliteRoundTripTests
{
    [Fact]
    public void Json_to_sqlite_round_trip_matches_json_reader_and_validates()
    {
        var json = Path.Combine(Path.GetTempPath(), $"aegis-eccm-rt-{Guid.NewGuid():N}.json");
        var db = Path.Combine(Path.GetTempPath(), $"aegis-eccm-rt-{Guid.NewGuid():N}.db");
        try
        {
            File.WriteAllText(json, """
            { "importBatchId": "eccm-rt", "sensors": [
              { "platformId": "p1", "sensorId": "r1", "basePd": 0.8, "radarScanType": "Aesa", "frequencyAgile": true, "radarTechGeneration": 5 },
              { "platformId": "p1", "sensorId": "r2", "basePd": 0.75, "radarScanType": "Mechanical", "radarTechGeneration": 3 },
              { "platformId": "p1", "sensorId": "r0", "basePd": 0.7 } ] }
            """);
            var fromJson = CatalogJsonImporter.ReadSensorBindings(json);
            CatalogJsonImporter.ImportToSqlite(json, db);

            IReadOnlyList<CatalogSensorBinding> fromSqlite;
            using (var reader = new SqliteCatalogReader(db))
            {
                fromSqlite = reader.GetSortedSensorBindings();
            }

            Assert.Equal(fromJson.Count, fromSqlite.Count);
            for (var i = 0; i < fromJson.Count; i++)
            {
                Assert.Equal(fromJson[i].SensorId, fromSqlite[i].SensorId);
                Assert.Equal(fromJson[i].RadarScanType, fromSqlite[i].RadarScanType);
                Assert.Equal(fromJson[i].FrequencyAgile, fromSqlite[i].FrequencyAgile);
                Assert.Equal(fromJson[i].RadarTechGeneration, fromSqlite[i].RadarTechGeneration);
                Assert.Empty(CatalogRadarScanTypes.Validate(fromSqlite[i]));
            }

            Assert.Equal("Aesa", fromSqlite[1].RadarScanType);
            Assert.True(fromSqlite[1].FrequencyAgile);
            Assert.Equal(5, fromSqlite[1].RadarTechGeneration);
            Assert.Equal(
                CatalogSnapshotHasher.ComputeSha256Hex(fromJson),
                CatalogSnapshotHasher.ComputeSha256Hex(fromSqlite));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(json);
            File.Delete(db);
        }
    }

    [Fact]
    public void Rows_without_eccm_read_back_as_unspecified()
    {
        var db = Path.Combine(Path.GetTempPath(), $"aegis-eccm-def-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.WriteSqlite(db, [new CatalogSensorBinding("p1", "r1", 0.9)]);
            using var reader = new SqliteCatalogReader(db);
            var row = Assert.Single(reader.GetSortedSensorBindings());
            Assert.Equal(CatalogRadarScanTypes.Unspecified, row.RadarScanType);
            Assert.False(row.FrequencyAgile);
            Assert.Equal(0, row.RadarTechGeneration);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(db);
        }
    }
}
