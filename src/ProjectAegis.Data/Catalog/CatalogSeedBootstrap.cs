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
            using var jsonConnection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
            jsonConnection.Open();
            SeedBalticPlatforms(jsonConnection);
            SeedBalticDamage(jsonConnection);
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

        SeedBalticPlatforms(connection);
        SeedBalticDamage(connection);
    }

    private static void SeedBalticPlatforms(SqliteConnection connection)
    {
        if (!TableExists(connection, "platform"))
        {
            return;
        }

        foreach (var platform in CatalogValidationDefaults.BalticPlatforms())
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR REPLACE INTO platform (platform_id, snapshot_id, lat_deg, lon_deg, combat_radius_nm)
                VALUES ($id, $snapshot, $lat, $lon, $radius)
                """;
            cmd.Parameters.AddWithValue("$id", platform.PlatformId);
            cmd.Parameters.AddWithValue("$snapshot", CatalogValidationDefaults.BalticSnapshotId);
            cmd.Parameters.AddWithValue("$lat", platform.LatDeg);
            cmd.Parameters.AddWithValue("$lon", platform.LonDeg);
            cmd.Parameters.AddWithValue("$radius", platform.CombatRadiusNm);
            cmd.ExecuteNonQuery();
        }

        using (var snap = connection.CreateCommand())
        {
            snap.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
            snap.Parameters.AddWithValue("$id", CatalogValidationDefaults.BalticSnapshotId);
            snap.ExecuteNonQuery();
        }
    }

    private static void SeedBalticDamage(SqliteConnection connection)
    {
        if (!TableExists(connection, "platform_damage"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_damage (platform_id, max_hp, withdraw_threshold_pct, critical_flags,
                review_state, trl_level, value_tier, citation_ref)
            VALUES ($id, $maxHp, $withdraw, $flags, $review, $trl, $tier, $citation)
            """;
        cmd.Parameters.AddWithValue("$id", "u1");
        cmd.Parameters.AddWithValue("$maxHp", 100);
        cmd.Parameters.AddWithValue("$withdraw", 25);
        cmd.Parameters.AddWithValue("$flags", 0);
        cmd.Parameters.AddWithValue("$review", CatalogReviewStates.Provisional);
        cmd.Parameters.AddWithValue("$trl", 9);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.GameplayAbstraction);
        cmd.Parameters.AddWithValue("$citation", string.Empty);
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}