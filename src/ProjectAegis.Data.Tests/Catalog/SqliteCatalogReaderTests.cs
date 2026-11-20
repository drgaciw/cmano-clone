using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[Runner.CatalogSqliteCollection]
public sealed class SqliteCatalogReaderTests
{
    [Test]
    public void Sqlite_reader_loads_sorted_sensor_bindings()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-catalog-{Guid.NewGuid():N}.db");
        try
        {
            using (new SqliteCatalogReader(dbPath))
            {
                using var insert = new SqliteConnection($"Data Source={dbPath};Pooling=false");
                insert.Open();
                using var cmd = insert.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                        import_batch_id, source_file, review_state, trl_level)
                    VALUES ('u2', 'radar-b', 0.5, 'test', 1.0, '', '', 'approved', 9),
                           ('u1', 'radar-a', 0.7, 'test', 1.0, '', '', 'approved', 9)
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var reader = new SqliteCatalogReader(dbPath))
            {
                var bindings = reader.GetSortedSensorBindings();
                Assert.That(bindings.Count, Is.EqualTo(2));
                Assert.That(bindings[0].PlatformId, Is.EqualTo("u1"));
                Assert.That(bindings[0].SensorId, Is.EqualTo("radar-a"));
                Assert.That(reader.TryGetBasePd("u2", "radar-b", out var basePd), Is.True);
                Assert.That(basePd, Is.EqualTo(0.5));
            }
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