using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>DRG-416: radar ECCM attributes survive JSON -> SQLite -> reader unchanged.</summary>
public sealed class CatalogRadarEccmSqliteRoundTripTests
{
    [Test]
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

            Assert.That(fromSqlite.Count, Is.EqualTo(fromJson.Count));
            for (var i = 0; i < fromJson.Count; i++)
            {
                Assert.That(fromSqlite[i].SensorId, Is.EqualTo(fromJson[i].SensorId));
                Assert.That(fromSqlite[i].RadarScanType, Is.EqualTo(fromJson[i].RadarScanType));
                Assert.That(fromSqlite[i].FrequencyAgile, Is.EqualTo(fromJson[i].FrequencyAgile));
                Assert.That(fromSqlite[i].RadarTechGeneration, Is.EqualTo(fromJson[i].RadarTechGeneration));
                Assert.That(CatalogRadarScanTypes.Validate(fromSqlite[i]), Is.Empty);
            }

            Assert.That(fromSqlite[1].RadarScanType, Is.EqualTo("Aesa"));
            Assert.That(fromSqlite[1].FrequencyAgile, Is.True);
            Assert.That(fromSqlite[1].RadarTechGeneration, Is.EqualTo(5));
            Assert.That(                CatalogSnapshotHasher.ComputeSha256Hex(fromSqlite), Is.EqualTo(                CatalogSnapshotHasher.ComputeSha256Hex(fromJson)));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(json);
            File.Delete(db);
        }
    }

    [Test]
    public void Rows_without_eccm_read_back_as_unspecified()
    {
        var db = Path.Combine(Path.GetTempPath(), $"aegis-eccm-def-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.WriteSqlite(db, [new CatalogSensorBinding("p1", "r1", 0.9)]);
            using var reader = new SqliteCatalogReader(db);
            var row = ProjectAegis.Data.Tests.AssertEx.Single(reader.GetSortedSensorBindings());
            Assert.That(row.RadarScanType, Is.EqualTo(CatalogRadarScanTypes.Unspecified));
            Assert.That(row.FrequencyAgile, Is.False);
            Assert.That(row.RadarTechGeneration, Is.EqualTo(0));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(db);
        }
    }
}
