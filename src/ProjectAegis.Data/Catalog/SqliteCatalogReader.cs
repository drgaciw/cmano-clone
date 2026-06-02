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
        _connection = new SqliteConnection($"Data Source={databasePath}");
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

    public void Dispose() => _connection.Dispose();

    private void ApplyMigrations()
    {
        var migrationPath = ResolveMigrationPath();
        if (!File.Exists(migrationPath))
        {
            throw new FileNotFoundException($"Catalog migration not found: {migrationPath}");
        }

        var sql = File.ReadAllText(migrationPath);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private CatalogSensorBinding[] LoadSorted()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence
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
                reader.GetDouble(4)));
        }

        return list.ToArray();
    }

    private static string ResolveMigrationPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "data", "catalog", "migrations", "001_sensor_base_pd.sql");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine("assets", "data", "catalog", "migrations", "001_sensor_base_pd.sql");
    }
}