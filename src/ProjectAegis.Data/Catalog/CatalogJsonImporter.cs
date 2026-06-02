namespace ProjectAegis.Data.Catalog;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

/// <summary>Imports catalog sensor rows from JSON into SQLite (DATA-2 file drop).</summary>
public static class CatalogJsonImporter
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static IReadOnlyList<CatalogSensorBinding> ReadSensorBindings(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Catalog JSON not found: {jsonPath}");
        }

        var dto = JsonSerializer.Deserialize<CatalogSensorsFileDto>(
            File.ReadAllText(jsonPath),
            JsonOptions) ?? throw new InvalidDataException("Catalog JSON deserialized to null.");

        return dto.Sensors
            .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
            .ThenBy(s => s.SensorId, StringComparer.Ordinal)
            .Select(s => new CatalogSensorBinding(
                s.PlatformId,
                s.SensorId,
                s.BasePd,
                s.SourceFactId,
                s.Confidence))
            .ToArray();
    }

    public static void WriteSqlite(string databasePath, IReadOnlyList<CatalogSensorBinding> bindings, bool overwrite = true)
    {
        if (overwrite && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        using (var _ = new SqliteCatalogReader(databasePath, "p0-json-write"))
        {
        }

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        foreach (var sensor in bindings)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                INSERT OR REPLACE INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence)
                VALUES ($platform, $sensor, $basePd, $source, $confidence)
                """;
            cmd.Parameters.AddWithValue("$platform", sensor.PlatformId);
            cmd.Parameters.AddWithValue("$sensor", sensor.SensorId);
            cmd.Parameters.AddWithValue("$basePd", sensor.BasePd);
            cmd.Parameters.AddWithValue("$source", sensor.SourceFactId);
            cmd.Parameters.AddWithValue("$confidence", sensor.Confidence);
            cmd.ExecuteNonQuery();
        }
    }

    public static void ImportToSqlite(string jsonPath, string databasePath, bool overwrite = true) =>
        WriteSqlite(databasePath, ReadSensorBindings(jsonPath), overwrite);

    public static string ResolveBalticSensorsJsonPath() => ResolveRepoRelative(
        Path.Combine("assets", "data", "catalog", "sensors_baltic.json"));

    internal static string ResolveRepoRelative(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return relativePath;
    }

    internal sealed class CatalogSensorsFileDto
    {
        [JsonPropertyName("sensors")]
        public List<CatalogSensorRowDto> Sensors { get; init; } = [];
    }

    internal sealed class CatalogSensorRowDto
    {
        public string PlatformId { get; init; } = "";

        public string SensorId { get; init; } = "";

        public double BasePd { get; init; }

        public string SourceFactId { get; init; } = "catalog-import";

        public double Confidence { get; init; } = 1.0;
    }
}