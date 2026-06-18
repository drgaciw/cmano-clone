namespace ProjectAegis.Data.Catalog;

using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Platform;

/// <summary>SQLite-backed catalog reader; applies migrations on open.</summary>
public sealed class SqliteCatalogReader : ICatalogReader, IDisposable
{
    private static readonly HashSet<string> PragmaTableWhitelist = new(StringComparer.Ordinal)
    {
        "sensor",
        "sensor_quarantine",
    };
    private readonly SqliteConnection _connection;
    private CatalogSensorBinding[]? _cache;
    private Dictionary<string, CatalogPlatformEntry>? _platforms;
    private HashSet<string>? _snapshots;
    private bool? _hasPlatformTable;
    private CatalogMobility[]? _mobilityCache;
    private CatalogSignature[]? _signatureCache;
    private CatalogEmcon[]? _emconCache;
    private CatalogPlatformDamage[]? _damageCache;
    private CatalogMount[]? _mountsCache;
    private CatalogLoadout[]? _loadoutsCache;
    private CatalogMagazineEntry[]? _magazinesCache;

    public SqliteCatalogReader(string databasePath, string layerVersion = "p0-sqlite")
    {
        LayerVersion = layerVersion;
        _connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        _connection.Open();
        ApplyMigrations();
    }

    public string LayerVersion { get; }

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings()
    {
        _cache ??= LoadSorted();
        return _cache;
    }

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
    {
        foreach (var binding in GetSortedSensorBindings())
        {
            if (string.Equals(binding.PlatformId, platformId, StringComparison.Ordinal) &&
                string.Equals(binding.SensorId, sensorId, StringComparison.Ordinal))
            {
                basePd = binding.BasePd;
                return true;
            }
        }

        basePd = 0;
        return false;
    }

    public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
    {
        EnsureSnapshotsLoaded();
        if (_snapshots!.Contains(dbRef))
        {
            resolvedSnapshotId = dbRef;
            return true;
        }

        return CatalogValidationDefaults.TryResolveBalticDbRef(dbRef, out resolvedSnapshotId);
    }

    public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm)
    {
        if (TryGetPlatform(platformId, out var entry))
        {
            combatRadiusNm = entry.CombatRadiusNm;
            return true;
        }

        combatRadiusNm = 0;
        return false;
    }

    public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg)
    {
        if (TryGetPlatform(platformId, out var entry))
        {
            latDeg = entry.LatDeg;
            lonDeg = entry.LonDeg;
            return true;
        }

        latDeg = 0;
        lonDeg = 0;
        return false;
    }

    public bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope) =>
        CatalogWeaponDefaults.TryResolve(weaponId, out envelope);

    public IReadOnlyList<CatalogMobility> GetSortedMobility()
    {
        _mobilityCache ??= LoadMobilitySorted();
        return _mobilityCache;
    }

    public IReadOnlyList<CatalogSignature> GetSortedSignatures()
    {
        _signatureCache ??= LoadSignaturesSorted();
        return _signatureCache;
    }

    public IReadOnlyList<CatalogEmcon> GetSortedEmcon()
    {
        _emconCache ??= LoadEmconSorted();
        return _emconCache;
    }

    public bool TryGetMobility(string platformId, out CatalogMobility mobility)
    {
        foreach (var row in GetSortedMobility())
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                mobility = row;
                return true;
            }
        }

        mobility = new CatalogMobility(platformId);
        return false;
    }

    public bool TryGetSignature(string platformId, out CatalogSignature signature)
    {
        foreach (var row in GetSortedSignatures())
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                signature = row;
                return true;
            }
        }

        signature = new CatalogSignature(platformId);
        return false;
    }

    public bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon)
    {
        foreach (var row in GetSortedEmcon())
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal) &&
                string.Equals(row.Condition, condition, StringComparison.Ordinal) &&
                string.Equals(row.EmitterId, emitterId, StringComparison.Ordinal))
            {
                emcon = row;
                return true;
            }
        }

        emcon = new CatalogEmcon(platformId, condition, emitterId);
        return false;
    }

    public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage()
    {
        _damageCache ??= LoadDamageSorted();
        return _damageCache;
    }

    public bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage)
    {
        foreach (var row in GetSortedPlatformDamage())
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                damage = row;
                return true;
            }
        }

        damage = new CatalogPlatformDamage(platformId);
        return false;
    }

    public IReadOnlyList<CatalogMount> GetSortedMounts()
    {
        _mountsCache ??= LoadMountsSorted().ToArray();
        return _mountsCache;
    }

    public IReadOnlyList<CatalogLoadout> GetSortedLoadouts()
    {
        _loadoutsCache ??= LoadLoadoutsSorted().ToArray();
        return _loadoutsCache;
    }

    public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines()
    {
        _magazinesCache ??= LoadMagazinesSorted().ToArray();
        return _magazinesCache;
    }

    /// <summary>Req-21: build workbook export payload from the bound SQLite snapshot.</summary>
    public PlatformCatalogExportData LoadExportData()
    {
        EnsurePlatformsLoaded();
        return new PlatformCatalogExportData(
            Platforms: _platforms!.Values.OrderBy(p => p.PlatformId, StringComparer.Ordinal).ToArray(),
            Sensors: GetSortedSensorBindings(),
            Mounts: LoadMountsSorted(),
            Loadouts: LoadLoadoutsSorted(),
            Magazines: LoadMagazinesSorted(),
            Comms: LoadCommsSorted(),
            Mobility: GetSortedMobility(),
            Signatures: GetSortedSignatures(),
            Emcon: GetSortedEmcon(),
            Damage: GetSortedPlatformDamage());
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }

    private bool MigrationColumnExists(string table, string column)
    {
        if (!IsSafeSqlIdentifier(table) || !IsSafeSqlIdentifier(column) || !TableExists(table))
        {
            return false;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private void ApplyMigrations()
    {
        foreach (var migrationPath in ResolveMigrationPaths())
        {
            if (ShouldSkipMigration(migrationPath))
            {
                continue;
            }

            var sql = File.ReadAllText(migrationPath);
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    private bool ShouldSkipMigration(string migrationPath)
    {
        var file = Path.GetFileName(migrationPath);
        if (file.Contains("002", StringComparison.Ordinal) && TableHasColumn("sensor", "review_state"))
        {
            return true;
        }

        if (file.Contains("003", StringComparison.Ordinal) && TableExists("sensor_quarantine"))
        {
            return true;
        }

        if (file.Contains("004", StringComparison.Ordinal) && TableExists("platform"))
        {
            return true;
        }

        if (file.Contains("005", StringComparison.Ordinal) && TableExists("catalog_change_log"))
        {
            return true;
        }

        if (file.Contains("006", StringComparison.Ordinal) && MigrationColumnExists("catalog_snapshot", "content_hash_sha256"))
        {
            return true;
        }

        if (file.Contains("007", StringComparison.Ordinal) && TableExists("platform_mount"))
        {
            return true;
        }

        if (file.Contains("008", StringComparison.Ordinal) && TableExists("platform_mobility"))
        {
            return true;
        }

        if (file.Contains("009", StringComparison.Ordinal) && TableExists("platform_damage"))
        {
            return true;
        }

        if (file.Contains("010", StringComparison.Ordinal) && MigrationColumnExists("catalog_snapshot", "branch"))
        {
            return true;
        }

        return false;
    }

    private bool TableExists(string table)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private bool TableHasColumn(string table, string column)
    {
        if (!PragmaTableWhitelist.Contains(table) || !IsSafeSqlIdentifier(column))
        {
            return false;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static bool IsSafeSqlIdentifier(string value) =>
        !string.IsNullOrEmpty(value) && value.All(static c => char.IsLetterOrDigit(c) || c == '_');

    private CatalogSensorBinding[] LoadSorted()
    {
        using var cmd = _connection.CreateCommand();
        var hasProvenance = TableHasColumn("sensor", "value_tier");
        cmd.CommandText = hasProvenance
            ? """
              SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence,
                     import_batch_id, source_file, review_state, trl_level,
                     value_tier, reviewer_id, revised_utc_ticks, citation_ref
              FROM sensor
              ORDER BY platform_id ASC, sensor_id ASC
              """
            : """
              SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence,
                     import_batch_id, source_file, review_state, trl_level
              FROM sensor
              ORDER BY platform_id ASC, sensor_id ASC
              """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogSensorBinding>();
        while (reader.Read())
        {
            if (hasProvenance)
            {
                list.Add(new CatalogSensorBinding(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetDouble(2),
                    reader.GetString(3),
                    reader.GetDouble(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetInt32(8),
                    CatalogProvenanceTier.Normalize(reader.GetString(9)),
                    reader.GetString(10),
                    reader.GetInt64(11),
                    reader.GetString(12)));
            }
            else
            {
                list.Add(new CatalogSensorBinding(
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
        }

        return list.ToArray();
    }

    private static IReadOnlyList<string> ResolveMigrationPaths()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var migrationsDir = Path.Combine(dir.FullName, "assets", "data", "catalog", "migrations");
            if (Directory.Exists(migrationsDir))
            {
                return Directory
                    .EnumerateFiles(migrationsDir, "*.sql", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
            }

            dir = dir.Parent;
        }

        return [Path.Combine("assets", "data", "catalog", "migrations", "001_sensor_base_pd.sql")];
    }

    private void EnsureSnapshotsLoaded()
    {
        if (_snapshots != null)
        {
            return;
        }

        _snapshots = new HashSet<string>(StringComparer.Ordinal);
        if (!TableExists("catalog_snapshot"))
        {
            _snapshots.Add(CatalogValidationDefaults.BalticSnapshotId);
            return;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT snapshot_id FROM catalog_snapshot ORDER BY snapshot_id ASC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            _snapshots.Add(reader.GetString(0));
        }

        if (_snapshots.Count == 0)
        {
            _snapshots.Add(CatalogValidationDefaults.BalticSnapshotId);
        }
    }

    private bool TryGetPlatform(string platformId, out CatalogPlatformEntry entry)
    {
        EnsurePlatformsLoaded();
        return _platforms!.TryGetValue(platformId, out entry!);
    }

    private void EnsurePlatformsLoaded()
    {
        if (_platforms != null)
        {
            return;
        }

        _platforms = new Dictionary<string, CatalogPlatformEntry>(StringComparer.Ordinal);
        if (PlatformTableExists())
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT platform_id, lat_deg, lon_deg, combat_radius_nm
                FROM platform
                ORDER BY platform_id ASC, snapshot_id ASC
                """;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetString(0);
                if (!_platforms.ContainsKey(id))
                {
                    _platforms[id] = new CatalogPlatformEntry(
                        id,
                        reader.GetDouble(1),
                        reader.GetDouble(2),
                        reader.GetDouble(3));
                }
            }
        }

        if (_platforms.Count == 0)
        {
            foreach (var platform in CatalogValidationDefaults.BalticPlatforms())
            {
                _platforms[platform.PlatformId] = platform;
            }
        }
    }

    private bool PlatformTableExists()
    {
        _hasPlatformTable ??= TableExists("platform");
        return _hasPlatformTable.Value;
    }

    private CatalogMobility[] LoadMobilitySorted()
    {
        if (!TableExists("platform_mobility"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, max_speed_knots, cruise_speed_knots, max_altitude_ft, max_depth_m,
                   fuel_capacity, range_nm, endurance_hr, review_state, trl_level, value_tier, citation_ref
            FROM platform_mobility
            ORDER BY platform_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMobility>();
        while (reader.Read())
        {
            list.Add(new CatalogMobility(
                reader.GetString(0),
                reader.GetDouble(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetDouble(5),
                reader.GetDouble(6),
                reader.GetDouble(7),
                reader.GetString(8),
                reader.GetInt32(9),
                CatalogProvenanceTier.Normalize(reader.GetString(10)),
                reader.GetString(11)));
        }

        return list.ToArray();
    }

    private CatalogSignature[] LoadSignaturesSorted()
    {
        if (!TableExists("platform_signature"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, rcs_band_dbsm, ir_signature, acoustic_signature_db, magnetic_signature,
                   review_state, trl_level, value_tier, citation_ref
            FROM platform_signature
            ORDER BY platform_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogSignature>();
        while (reader.Read())
        {
            list.Add(new CatalogSignature(
                reader.GetString(0),
                reader.GetDouble(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetString(5),
                reader.GetInt32(6),
                CatalogProvenanceTier.Normalize(reader.GetString(7)),
                reader.GetString(8)));
        }

        return list.ToArray();
    }

    private CatalogEmcon[] LoadEmconSorted()
    {
        if (!TableExists("platform_emcon"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, condition, emitter_id, posture, review_state
            FROM platform_emcon
            ORDER BY platform_id ASC, condition ASC, emitter_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogEmcon>();
        while (reader.Read())
        {
            list.Add(new CatalogEmcon(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return list.ToArray();
    }

    private CatalogPlatformDamage[] LoadDamageSorted()
    {
        if (!TableExists("platform_damage"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, max_hp, withdraw_threshold_pct, critical_flags,
                   review_state, trl_level, value_tier, citation_ref
            FROM platform_damage
            ORDER BY platform_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogPlatformDamage>();
        while (reader.Read())
        {
            list.Add(new CatalogPlatformDamage(
                reader.GetString(0),
                reader.GetDouble(1),
                reader.GetDouble(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetInt32(5),
                CatalogProvenanceTier.Normalize(reader.GetString(6)),
                reader.GetString(7)));
        }

        return list.ToArray();
    }

    private IReadOnlyList<CatalogMount> LoadMountsSorted()
    {
        if (!TableExists("platform_mount"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, mount_id, mount_type, arc_deg, capacity, review_state
            FROM platform_mount
            ORDER BY platform_id ASC, mount_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMount>();
        while (reader.Read())
        {
            list.Add(new CatalogMount(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.GetString(5)));
        }

        return list;
    }

    private IReadOnlyList<CatalogLoadout> LoadLoadoutsSorted()
    {
        if (!TableExists("platform_loadout"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, loadout_name, role, is_default
            FROM platform_loadout
            ORDER BY platform_id ASC, loadout_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogLoadout>();
        while (reader.Read())
        {
            list.Add(new CatalogLoadout(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4) == 1));
        }

        return list;
    }

    private IReadOnlyList<CatalogMagazineEntry> LoadMagazinesSorted()
    {
        if (!TableExists("platform_magazine"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth
            FROM platform_magazine
            ORDER BY platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMagazineEntry>();
        while (reader.Read())
        {
            list.Add(new CatalogMagazineEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6)));
        }

        return list;
    }

    private IReadOnlyList<CatalogCommsBinding> LoadCommsSorted()
    {
        if (!TableExists("platform_comms"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, link_id, role, satcom_capable, review_state, trl_level, value_tier, citation_ref
            FROM platform_comms
            ORDER BY platform_id ASC, link_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogCommsBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogCommsBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7)));
        }

        return list;
    }
}