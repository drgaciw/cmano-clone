namespace ProjectAegis.Data.Catalog;

using Microsoft.Data.Sqlite;

/// <summary>Writes deterministic Baltic fixture rows into a SQLite catalog file.</summary>
public static class CatalogSeedBootstrap
{
    public static void SeedBalticPatrol(string databasePath, bool overwrite = true)
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        if (File.Exists(jsonPath))
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, databasePath, overwrite);
            return;
        }

        if (overwrite && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        using (var _ = new SqliteCatalogReader(databasePath, "p0-seed"))
        {
        }

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        foreach (var binding in InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings())
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR REPLACE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                    import_batch_id, source_file, review_state, trl_level)
                VALUES ($platform, $sensor, $basePd, $source, $confidence, $batch, $file, $review, $trl)
                """;
            cmd.Parameters.AddWithValue("$platform", binding.PlatformId);
            cmd.Parameters.AddWithValue("$sensor", binding.SensorId);
            cmd.Parameters.AddWithValue("$basePd", binding.BasePd);
            cmd.Parameters.AddWithValue("$source", binding.SourceFactId);
            cmd.Parameters.AddWithValue("$confidence", binding.Confidence);
            cmd.Parameters.AddWithValue("$batch", binding.ImportBatchId);
            cmd.Parameters.AddWithValue("$file", binding.SourceFile);
            cmd.Parameters.AddWithValue("$review", binding.ReviewState);
            cmd.Parameters.AddWithValue("$trl", binding.TrlLevel);
            cmd.ExecuteNonQuery();
        }
    }
}