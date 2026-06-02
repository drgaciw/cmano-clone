namespace ProjectAegis.Data.Catalog;

using Microsoft.Data.Sqlite;

/// <summary>SQLite-backed catalog reader; applies migrations on open.</summary>
public sealed class SqliteCatalogReader : ICatalogReader, IDisposable
{
    private readonly SqliteConnection _connection;
    private CatalogSensorBinding[]? _cache;

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

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
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
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private CatalogSensorBinding[] LoadSorted()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence,
                   import_batch_id, source_file, review_state, trl_level
            FROM sensor
            ORDER BY platform_id ASC, sensor_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogSensorBinding>();
        while (reader.Read())
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
}