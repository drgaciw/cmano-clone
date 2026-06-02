using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class SqliteCatalogReaderTests
{
    [Fact]
    public void Sqlite_reader_loads_sorted_sensor_bindings()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-catalog-{Guid.NewGuid():N}.db");
        try
        {
            using (var bootstrap = new SqliteCatalogReader(dbPath))
            {
                using var insert = new SqliteConnection($"Data Source={dbPath}");
                insert.Open();
                using var cmd = insert.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence)
                    VALUES ('u2', 'radar-b', 0.5, 'test', 1.0),
                           ('u1', 'radar-a', 0.7, 'test', 1.0)
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var reader = new SqliteCatalogReader(dbPath))
            {
                var bindings = reader.GetSortedSensorBindings();
                Assert.Equal(2, bindings.Count);
                Assert.Equal("u1", bindings[0].PlatformId);
                Assert.Equal("radar-a", bindings[0].SensorId);
                Assert.True(reader.TryGetBasePd("u2", "radar-b", out var basePd));
                Assert.Equal(0.5, basePd);
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