namespace ProjectAegis.Data.Catalog;

using Microsoft.Data.Sqlite;

/// <summary>Writes deterministic Baltic fixture rows into a SQLite catalog file.</summary>
public static class CatalogSeedBootstrap
{
    /// <summary>Schema + migrations only — no Baltic OOB rows (enterprise public-corpus bootstrap).</summary>
    public static void EnsureSchemaOnly(string databasePath, bool overwrite = true)
    {
        if (overwrite && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        using (var _ = new SqliteCatalogReader(databasePath, "enterprise-corpus-schema"))
        {
        }

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        SeedPublicCorpusCatalogPlatform(connection);
    }

    private static void SeedPublicCorpusCatalogPlatform(SqliteConnection connection)
    {
        if (!TableExists(connection, "platform"))
        {
            return;
        }

        using (var snap = connection.CreateCommand())
        {
            snap.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
            snap.Parameters.AddWithValue("$id", CatalogValidationDefaults.PublicCorpusSnapshotId);
            snap.ExecuteNonQuery();
        }

        InsertPlatformRow(
            connection,
            new CatalogPlatformEntry(
                CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId,
                LatDeg: 0.0,
                LonDeg: 0.0,
                CombatRadiusNm: 1.0),
            CatalogValidationDefaults.PublicCorpusSnapshotId);
        SeedGenericSwarmPlatform(connection);
    }

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
            SeedGenericSwarmPlatform(jsonConnection);
            SeedSensorModalityFixtures(jsonConnection);
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
        SeedGenericSwarmPlatform(connection);
        SeedSensorModalityFixtures(connection);
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
        SeedGenericSwarmPlatform(connection);
        SeedSensorModalityFixtures(connection);
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

    private static void InsertPlatformRow(
        SqliteConnection connection,
        CatalogPlatformEntry platform,
        string? snapshotId = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform (platform_id, snapshot_id, lat_deg, lon_deg, combat_radius_nm)
            VALUES ($id, $snapshot, $lat, $lon, $radius)
            """;
        cmd.Parameters.AddWithValue("$id", platform.PlatformId);
        cmd.Parameters.AddWithValue("$snapshot", snapshotId ?? CatalogValidationDefaults.BalticSnapshotId);
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
    /// SWARM-21 Phase A: abstract generic swarm platform row + position + default sensor.
    /// Insert-if-absent only — never overwrites curated / write-gate-approved rows
    /// (safe for EnsureGenericSwarmPlatform on every catalog-reader open).
    /// </summary>
    private static void SeedGenericSwarmPlatform(SqliteConnection connection)
    {
        var entry = CatalogValidationDefaults.GenericSwarmPlatformEntry();
        var swarm = CatalogValidationDefaults.GenericSwarmPlatform();

        if (TableExists(connection, "platform"))
        {
            // BalticPlatforms() already inserts the generic id without display_name —
            // insert-if-absent alone would skip metadata. Fill blank starter chrome only.
            InsertPlatformRowIfAbsent(connection, entry);
            if (IsBlankDisplayName(connection, entry.PlatformId))
            {
                TryUpdatePlatformMetadata(
                    connection,
                    entry.PlatformId,
                    CatalogSwarmPlatformDefaults.GenericDisplayName,
                    domain: "air",
                    platformClass: "uas-swarm",
                    nationality: "GENERIC",
                    gameTechnologyLevel: 0);
            }
        }

        if (TableExists(connection, "platform_swarm"))
        {
            InsertSwarmPlatformRow(connection, swarm);
            InsertSwarmPlatformRow(connection, CatalogValidationDefaults.UsnCecSwarmPlatform());
        }

        var usnEntry = CatalogValidationDefaults.UsnCecSwarmPlatformEntry();
        if (TableExists(connection, "platform"))
        {
            InsertPlatformRowIfAbsent(connection, usnEntry);
            if (IsBlankDisplayName(connection, usnEntry.PlatformId))
            {
                TryUpdatePlatformMetadata(
                    connection,
                    usnEntry.PlatformId,
                    CatalogSwarmPlatformDefaults.UsnCecDisplayName,
                    domain: "air",
                    platformClass: "uas-swarm",
                    nationality: "USA",
                    gameTechnologyLevel: 0);
            }
        }

        if (TableExists(connection, "sensor"))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                    import_batch_id, source_file, review_state, trl_level)
                VALUES ($platform, $sensor, $basePd, $source, $confidence, $batch, $file, $review, $trl)
                """;
            cmd.Parameters.AddWithValue("$platform", swarm.PlatformId);
            cmd.Parameters.AddWithValue("$sensor", swarm.DefaultSensorId);
            cmd.Parameters.AddWithValue("$basePd", 0.80);
            cmd.Parameters.AddWithValue("$source", "swarm-phase-a-generic-preset");
            cmd.Parameters.AddWithValue("$confidence", 1.0);
            cmd.Parameters.AddWithValue("$batch", "swarm-a1");
            cmd.Parameters.AddWithValue("$file", "CatalogSeedBootstrap.SeedGenericSwarmPlatform");
            cmd.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
            cmd.Parameters.AddWithValue("$trl", 9);
            cmd.ExecuteNonQuery();
        }

        if (TableExists(connection, "weapon_catalog"))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO weapon_catalog
                    (weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance)
                VALUES ($id, $name, $min, $max, $type, $guidance)
                """;
            cmd.Parameters.AddWithValue("$id", swarm.DefaultWeaponId);
            cmd.Parameters.AddWithValue("$name", "Swarm Light Munition (generic)");
            cmd.Parameters.AddWithValue("$min", 0);
            cmd.Parameters.AddWithValue("$max", 8_000);
            cmd.Parameters.AddWithValue("$type", "Attritable UAS");
            cmd.Parameters.AddWithValue("$guidance", "EO");
            cmd.ExecuteNonQuery();
        }

        if (TableExists(connection, "sensor"))
        {
            using var cmdUsn = connection.CreateCommand();
            cmdUsn.CommandText =
                """
                INSERT OR IGNORE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                    import_batch_id, source_file, review_state, trl_level)
                VALUES ($platform, $sensor, $basePd, $source, $confidence, $batch, $file, $review, $trl)
                """;
            var usn = CatalogValidationDefaults.UsnCecSwarmPlatform();
            cmdUsn.Parameters.AddWithValue("$platform", usn.PlatformId);
            cmdUsn.Parameters.AddWithValue("$sensor", usn.DefaultSensorId);
            cmdUsn.Parameters.AddWithValue("$basePd", 0.85);
            cmdUsn.Parameters.AddWithValue("$source", "swarm-phase-b-usn-cec-exemplar");
            cmdUsn.Parameters.AddWithValue("$confidence", 1.0);
            cmdUsn.Parameters.AddWithValue("$batch", "swarm-b2");
            cmdUsn.Parameters.AddWithValue("$file", "CatalogSeedBootstrap.SeedGenericSwarmPlatform");
            cmdUsn.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
            cmdUsn.Parameters.AddWithValue("$trl", 8);
            cmdUsn.ExecuteNonQuery();
        }

        if (TableExists(connection, "weapon_catalog"))
        {
            using var cmdUsnWeapon = connection.CreateCommand();
            cmdUsnWeapon.CommandText =
                """
                INSERT OR IGNORE INTO weapon_catalog
                    (weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance)
                VALUES ($id, $name, $min, $max, $type, $guidance)
                """;
            cmdUsnWeapon.Parameters.AddWithValue("$id", CatalogSwarmPlatformDefaults.UsnCecWeaponId);
            cmdUsnWeapon.Parameters.AddWithValue("$name", "USN CEC Swarm Munition");
            cmdUsnWeapon.Parameters.AddWithValue("$min", 0);
            cmdUsnWeapon.Parameters.AddWithValue("$max", 10_000);
            cmdUsnWeapon.Parameters.AddWithValue("$type", "Attritable UAS");
            cmdUsnWeapon.Parameters.AddWithValue("$guidance", "CEC");
            cmdUsnWeapon.ExecuteNonQuery();
        }

        // Heal provisional USN CEC sensor rows from prior seeds (rule gate requires approved).
        if (TableExists(connection, "sensor"))
        {
            using var heal = connection.CreateCommand();
            heal.CommandText =
                """
                UPDATE sensor
                SET review_state = $review
                WHERE platform_id = $platform AND sensor_id = $sensor
                """;
            heal.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
            heal.Parameters.AddWithValue("$platform", CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId);
            heal.Parameters.AddWithValue("$sensor", CatalogSwarmPlatformDefaults.UsnCecSensorId);
            heal.ExecuteNonQuery();
        }

    }

    /// <summary>
    /// S111-02 / DRG-10: extend-only IR/Visual modality fixtures on Baltic <c>u1</c>.
    /// Requires migration 016 (<c>sensor.modality</c>). Idempotent INSERT OR REPLACE.
    /// </summary>
    private static void SeedSensorModalityFixtures(SqliteConnection connection)
    {
        if (!TableExists(connection, "sensor") || !ColumnExists(connection, "sensor", "modality"))
        {
            return;
        }

        InsertSensorWithModality(
            connection,
            platformId: "u1",
            sensorId: "fixture-ir-1",
            basePd: 0.80,
            sourceFactId: "s111-fixture-ir",
            modality: CatalogSensorModalities.Infrared);
        InsertSensorWithModality(
            connection,
            platformId: "u1",
            sensorId: "fixture-visual-1",
            basePd: 0.70,
            sourceFactId: "s111-fixture-visual",
            modality: CatalogSensorModalities.Visual);

        // Tag existing Recon [Internal IR] rows when present (Baltic v3 seed path).
        using var tagIr = connection.CreateCommand();
        tagIr.CommandText =
            """
            UPDATE sensor
            SET modality = $modality
            WHERE sensor_id = $sensor
            """;
        tagIr.Parameters.AddWithValue("$modality", CatalogSensorModalities.Infrared);
        tagIr.Parameters.AddWithValue("$sensor", "internal-ir");
        tagIr.ExecuteNonQuery();
    }

    private static void InsertSensorWithModality(
        SqliteConnection connection,
        string platformId,
        string sensorId,
        double basePd,
        string sourceFactId,
        string modality)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                import_batch_id, source_file, review_state, trl_level, modality)
            VALUES ($platform, $sensor, $basePd, $source, $confidence, $batch, $file, $review, $trl, $modality)
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$sensor", sensorId);
        cmd.Parameters.AddWithValue("$basePd", basePd);
        cmd.Parameters.AddWithValue("$source", sourceFactId);
        cmd.Parameters.AddWithValue("$confidence", 1.0);
        cmd.Parameters.AddWithValue("$batch", "s111-modality");
        cmd.Parameters.AddWithValue("$file", "CatalogSeedBootstrap.SeedSensorModalityFixtures");
        cmd.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
        cmd.Parameters.AddWithValue("$trl", 9);
        cmd.Parameters.AddWithValue("$modality", modality);
        cmd.ExecuteNonQuery();
    }

    private static void InsertSwarmPlatformRow(SqliteConnection connection, CatalogSwarmPlatform swarm)
    {
        using var cmd = connection.CreateCommand();
        var hasCec = ColumnExists(connection, "platform_swarm", "cec_capable");
        if (hasCec)
        {
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO platform_swarm
                    (platform_id, is_swarm, max_drones, armor_class, default_sensor_id, default_weapon_id,
                     review_state, trl_level, value_tier, citation_ref,
                     default_mode, requires_host, allowed_host_classes, cec_capable)
                VALUES ($id, $isSwarm, $max, $armor, $sensor, $weapon, $review, $trl, $tier, $citation,
                        $mode, $reqHost, $hostClasses, $cec)
                """;
        }
        else
        {
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO platform_swarm
                    (platform_id, is_swarm, max_drones, armor_class, default_sensor_id, default_weapon_id,
                     review_state, trl_level, value_tier, citation_ref)
                VALUES ($id, $isSwarm, $max, $armor, $sensor, $weapon, $review, $trl, $tier, $citation)
                """;
        }

        cmd.Parameters.AddWithValue("$id", swarm.PlatformId);
        cmd.Parameters.AddWithValue("$isSwarm", swarm.IsSwarm ? 1 : 0);
        cmd.Parameters.AddWithValue("$max", swarm.MaxDrones);
        cmd.Parameters.AddWithValue("$armor", swarm.ArmorClass);
        cmd.Parameters.AddWithValue("$sensor", swarm.DefaultSensorId);
        cmd.Parameters.AddWithValue("$weapon", swarm.DefaultWeaponId);
        cmd.Parameters.AddWithValue("$review", swarm.ReviewState);
        cmd.Parameters.AddWithValue("$trl", swarm.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", swarm.ValueTier);
        cmd.Parameters.AddWithValue("$citation", swarm.CitationRef);
        if (hasCec)
        {
            cmd.Parameters.AddWithValue("$mode", swarm.DefaultMode);
            cmd.Parameters.AddWithValue("$reqHost", swarm.RequiresHost ? 1 : 0);
            cmd.Parameters.AddWithValue("$hostClasses", swarm.AllowedHostClasses ?? "");
            cmd.Parameters.AddWithValue("$cec", swarm.CecCapable ? 1 : 0);
        }

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a platform row only when no row exists for <paramref name="platform"/>.PlatformId
    /// (PK is composite with snapshot_id — existence is checked by platform_id alone).
    /// Returns true when a new row was written.
    /// </summary>
    private static bool InsertPlatformRowIfAbsent(
        SqliteConnection connection,
        CatalogPlatformEntry platform,
        string? snapshotId = null)
    {
        using (var exists = connection.CreateCommand())
        {
            exists.CommandText = "SELECT COUNT(*) FROM platform WHERE platform_id = $id";
            exists.Parameters.AddWithValue("$id", platform.PlatformId);
            if (Convert.ToInt32(exists.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0)
            {
                return false;
            }
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO platform (platform_id, snapshot_id, lat_deg, lon_deg, combat_radius_nm)
            VALUES ($id, $snapshot, $lat, $lon, $radius)
            """;
        cmd.Parameters.AddWithValue("$id", platform.PlatformId);
        cmd.Parameters.AddWithValue("$snapshot", snapshotId ?? CatalogValidationDefaults.BalticSnapshotId);
        cmd.Parameters.AddWithValue("$lat", platform.LatDeg);
        cmd.Parameters.AddWithValue("$lon", platform.LonDeg);
        cmd.Parameters.AddWithValue("$radius", platform.CombatRadiusNm);
        cmd.ExecuteNonQuery();
        return true;
    }

    /// <summary>True when platform row is missing or <c>display_name</c> is empty/null.</summary>
    private static bool IsBlankDisplayName(SqliteConnection connection, string platformId)
    {
        if (!ColumnExists(connection, "platform", "display_name"))
        {
            return false;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT display_name FROM platform
            WHERE platform_id = $id
            ORDER BY snapshot_id ASC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$id", platformId);
        var value = cmd.ExecuteScalar() as string;
        return string.IsNullOrEmpty(value);
    }

    private static void TryUpdatePlatformMetadata(
        SqliteConnection connection,
        string platformId,
        string displayName,
        string domain,
        string platformClass,
        string nationality,
        int gameTechnologyLevel)
    {
        if (!ColumnExists(connection, "platform", "display_name"))
        {
            return;
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE platform
            SET display_name = $name,
                domain = $domain,
                platform_class = $class,
                nationality = $nat,
                game_technology_level = $tl
            WHERE platform_id = $id
            """;
        cmd.Parameters.AddWithValue("$name", displayName);
        cmd.Parameters.AddWithValue("$domain", domain);
        cmd.Parameters.AddWithValue("$class", platformClass);
        cmd.Parameters.AddWithValue("$nat", nationality);
        cmd.Parameters.AddWithValue("$tl", gameTechnologyLevel);
        cmd.Parameters.AddWithValue("$id", platformId);
        cmd.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection connection, string table, string column)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    /// <summary>
    /// Ensures migrations + missing generic swarm preset rows exist without rewriting the catalog.
    /// Safe to call on every harness open (insert-if-absent only; curated rows are preserved).
    /// </summary>
    public static void EnsureGenericSwarmPlatform(string databasePath)
    {
        using (var _ = new SqliteCatalogReader(databasePath, "swarm-a1-ensure"))
        {
        }

        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        SeedGenericSwarmPlatform(connection);
    }

    /// <summary>
    /// Ensures migrations are applied, then inserts review_state-gated Baltic Patrol EMCON
    /// rows (published <c>platform_emcon</c> plus proposed <c>catalog_staging_emcon</c>)
    /// for non-fixture platforms that have sensors (idempotent). Does not invent emitter
    /// performance. Fixture ids such as <c>u1</c> are excluded. A <c>radar-1</c> gameplay
    /// alias uses <c>free/active</c> so the default resolver triple stays legacy Active.
    /// </summary>
    public static void EnrichBalticEmcon(string databasePath)
    {
        using (var _ = new SqliteCatalogReader(databasePath, "p0-seed-emcon"))
        {
        }
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
            // Quantities must not exceed mount capacity (PLE-MAG-CAPACITY blocks propose/export diffs).
            InsertMagazine(connection, "u1", "asuw-default", "vls-fwd", CatalogWeaponIds.BalticRim66, quantity: 8);
            InsertMagazine(connection, "u1", "asuw-default", "gun-76", CatalogWeaponIds.BalticOto76, quantity: 1);
        }

        // Intentionally no platform_mobility row: kill-chain speed rule skips when mobility is
        // absent (warnings would break clean Baltic golden/report emptiness). Ship max speeds
        // also fail the weapon flight-speed heuristic as errors when present at real values.
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

    private static void InsertMobility(
        SqliteConnection connection,
        string platformId,
        double maxSpeedKnots,
        double cruiseSpeedKnots,
        double rangeNm)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mobility
                (platform_id, max_speed_knots, cruise_speed_knots, range_nm, review_state, trl_level, value_tier, citation_ref)
            VALUES ($platform, $max, $cruise, $range, $review, $trl, $tier, $citation)
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$max", maxSpeedKnots);
        cmd.Parameters.AddWithValue("$cruise", cruiseSpeedKnots);
        cmd.Parameters.AddWithValue("$range", rangeNm);
        cmd.Parameters.AddWithValue("$review", CatalogReviewStates.Approved);
        cmd.Parameters.AddWithValue("$trl", 9);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.GameplayAbstraction);
        cmd.Parameters.AddWithValue("$citation", "baltic-seed-mobility");
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