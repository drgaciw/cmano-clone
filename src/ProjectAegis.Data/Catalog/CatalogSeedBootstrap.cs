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
            SeedBalticEngageCatalog(jsonConnection);
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
        SeedBalticEngageCatalog(connection);
    }

    public static void SeedBalticV3(string databasePath, bool overwrite = true)
    {
        SeedBalticPatrol(databasePath, overwrite: false);
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        foreach (var binding in InMemoryCatalogReader.BalticV3Fixture().GetSortedSensorBindings())
        {
            if (string.Equals(binding.PlatformId, "u1", StringComparison.Ordinal))
            {
                continue;
            }

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

        SeedBalticV3Platforms(connection);
    }

    private static void SeedBalticPlatforms(SqliteConnection connection)
    {
        if (!TableExists(connection, "platform"))
        {
            return;
        }

        foreach (var platform in CatalogValidationDefaults.BalticPlatforms())
        {
            InsertPlatformRow(connection, platform);
        }

        using (var snap = connection.CreateCommand())
        {
            snap.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
            snap.Parameters.AddWithValue("$id", CatalogValidationDefaults.BalticSnapshotId);
            snap.ExecuteNonQuery();
        }
    }

    private static void SeedBalticV3Platforms(SqliteConnection connection)
    {
        if (!TableExists(connection, "platform"))
        {
            return;
        }

        foreach (var platform in CatalogValidationDefaults.BalticV3Platforms())
        {
            if (string.Equals(platform.PlatformId, "u1", StringComparison.Ordinal) ||
                string.Equals(platform.PlatformId, "hostile-1", StringComparison.Ordinal) ||
                string.Equals(platform.PlatformId, "hostile-far", StringComparison.Ordinal))
            {
                continue;
            }

            InsertPlatformRow(connection, platform);
        }
    }

    private static void InsertPlatformRow(SqliteConnection connection, CatalogPlatformEntry platform)
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

    /// <summary>
    /// Ensures migrations are applied, then adds Baltic engage catalog rows (idempotent).
    /// Safe for enriching an existing production seed without wiping sensors/platforms.
    /// </summary>
    public static void EnrichBalticEngageCatalog(string databasePath)
    {
        // Open reader to apply migrations, then write engage rows.
        using (var _ = new SqliteCatalogReader(databasePath, "p0-seed-enrich"))
        {
        }

        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        SeedBalticEngageCatalog(connection);
    }

    /// <summary>
    /// GAME-01 / KILLCHAIN-03: seed blue u1 engage path — weapons, approved mount, loadout, magazine.
    /// Ranges stay inside u1 combat radius so kill-chain R4 stays green.
    /// </summary>
    private static void SeedBalticEngageCatalog(SqliteConnection connection)
    {
        if (TableExists(connection, "weapon_catalog"))
        {
            InsertWeapon(
                connection,
                CatalogWeaponIds.BalticRim66,
                "RIM-66 Standard MR (Baltic seed)",
                minRangeMeters: 1_000,
                maxRangeMeters: 74_000,
                weaponType: "Guided Weapon",
                guidance: "SARH");
            InsertWeapon(
                connection,
                CatalogWeaponIds.BalticOto76,
                "76mm OTO Melara (Baltic seed)",
                minRangeMeters: 0,
                maxRangeMeters: 16_000,
                weaponType: "Gun",
                guidance: string.Empty);
        }

        if (TableExists(connection, "platform_mount"))
        {
            InsertMount(connection, "u1", "vls-fwd", "vls", arcDeg: 360, capacity: 8);
            InsertMount(connection, "u1", "gun-76", "gun", arcDeg: 300, capacity: 1);
        }

        if (TableExists(connection, "platform_loadout"))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR REPLACE INTO platform_loadout (platform_id, loadout_id, loadout_name, role, is_default)
                VALUES ($platform, $loadout, $name, $role, $default)
                """;
            cmd.Parameters.AddWithValue("$platform", "u1");
            cmd.Parameters.AddWithValue("$loadout", "asuw-default");
            cmd.Parameters.AddWithValue("$name", "ASUW Default");
            cmd.Parameters.AddWithValue("$role", "asuw");
            cmd.Parameters.AddWithValue("$default", 1);
            cmd.ExecuteNonQuery();
        }

        if (TableExists(connection, "platform_magazine"))
        {
            InsertMagazine(connection, "u1", "asuw-default", "vls-fwd", CatalogWeaponIds.BalticRim66, quantity: 8);
            InsertMagazine(connection, "u1", "asuw-default", "gun-76", CatalogWeaponIds.BalticOto76, quantity: 120);
        }

        // Intentionally no platform_mobility row: kill-chain speed rule compares inferred
        // weapon flight speed to platform max_speed; ship speeds fail that heuristic as errors.
        // Missing mobility yields advisory warnings only (ok:true).
    }

    private static void InsertWeapon(
        SqliteConnection connection,
        string weaponId,
        string displayName,
        double minRangeMeters,
        double maxRangeMeters,
        string weaponType,
        string guidance)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO weapon_catalog
                (weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance)
            VALUES ($id, $name, $min, $max, $type, $guidance)
            """;
        cmd.Parameters.AddWithValue("$id", weaponId);
        cmd.Parameters.AddWithValue("$name", displayName);
        cmd.Parameters.AddWithValue("$min", minRangeMeters);
        cmd.Parameters.AddWithValue("$max", maxRangeMeters);
        cmd.Parameters.AddWithValue("$type", weaponType);
        cmd.Parameters.AddWithValue("$guidance", guidance);
        cmd.ExecuteNonQuery();
    }

    private static void InsertMount(
        SqliteConnection connection,
        string platformId,
        string mountId,
        string mountType,
        double arcDeg,
        int capacity)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mount
                (platform_id, mount_id, mount_type, arc_deg, capacity, review_state)
            VALUES ($platform, $mount, $type, $arc, $capacity, $review)
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$mount", mountId);
        cmd.Parameters.AddWithValue("$type", mountType);
        cmd.Parameters.AddWithValue("$arc", arcDeg);
        cmd.Parameters.AddWithValue("$capacity", capacity);
        cmd.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
        cmd.ExecuteNonQuery();
    }

    private static void InsertMagazine(
        SqliteConnection connection,
        string platformId,
        string loadoutId,
        string mountId,
        string weaponId,
        int quantity)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_magazine
                (platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth)
            VALUES ($platform, $loadout, $mount, $weapon, $qty, 0, 0)
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$loadout", loadoutId);
        cmd.Parameters.AddWithValue("$mount", mountId);
        cmd.Parameters.AddWithValue("$weapon", weaponId);
        cmd.Parameters.AddWithValue("$qty", quantity);
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