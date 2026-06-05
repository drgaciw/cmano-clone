namespace ProjectAegis.Data.Catalog;

using Microsoft.Data.Sqlite;

/// <summary>Moves reviewer-approved rows from <c>sensor_quarantine</c> into <c>sensor</c>.</summary>
public static class CatalogQuarantinePromoter
{
    public static int PromoteApproved(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();

        var candidates = ReadApprovedQuarantineRows(connection);
        var promoted = 0;
        foreach (var binding in candidates)
        {
            if (CatalogImportGate.PartitionForImport([binding]).Quarantined.Length > 0)
            {
                continue;
            }

            InsertSensor(connection, binding);
            DeleteQuarantineRow(connection, binding.PlatformId, binding.SensorId);
            promoted++;
        }

        SqliteConnection.ClearAllPools();
        return promoted;
    }

    private static List<CatalogSensorBinding> ReadApprovedQuarantineRows(SqliteConnection connection)
    {
        var rows = new List<CatalogSensorBinding>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence,
                   import_batch_id, source_file, review_state, trl_level
            FROM sensor_quarantine
            WHERE lower(review_state) = $approved
            ORDER BY platform_id, sensor_id
            """;
        cmd.Parameters.AddWithValue("$approved", CatalogReviewStates.Approved);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new CatalogSensorBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2),
                reader.GetString(3),
                reader.GetDouble(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetInt32(8)));
        }

        return rows;
    }

    private static void InsertSensor(SqliteConnection connection, CatalogSensorBinding sensor)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                import_batch_id, source_file, review_state, trl_level)
            VALUES ($platform, $sensor, $basePd, $source, $confidence, $batch, $file, $review, $trl)
            """;
        cmd.Parameters.AddWithValue("$platform", sensor.PlatformId);
        cmd.Parameters.AddWithValue("$sensor", sensor.SensorId);
        cmd.Parameters.AddWithValue("$basePd", sensor.BasePd);
        cmd.Parameters.AddWithValue("$source", sensor.SourceFactId);
        cmd.Parameters.AddWithValue("$confidence", sensor.Confidence);
        cmd.Parameters.AddWithValue("$batch", sensor.ImportBatchId);
        cmd.Parameters.AddWithValue("$file", sensor.SourceFile);
        cmd.Parameters.AddWithValue("$review", sensor.ReviewState);
        cmd.Parameters.AddWithValue("$trl", sensor.TrlLevel);
        cmd.ExecuteNonQuery();
    }

    private static void DeleteQuarantineRow(SqliteConnection connection, string platformId, string sensorId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "DELETE FROM sensor_quarantine WHERE platform_id = $platform AND sensor_id = $sensor";
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$sensor", sensorId);
        cmd.ExecuteNonQuery();
    }
}