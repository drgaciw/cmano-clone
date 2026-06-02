namespace ProjectAegis.Data.Catalog;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

/// <summary>Imports catalog sensor rows from JSON into SQLite (DATA-2 file drop).</summary>
public static class CatalogJsonImporter
{
    public static void ImportToSqlite(string jsonPath, string databasePath, bool overwrite = true)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Catalog JSON not found: {jsonPath}");
        }

        if (overwrite && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        using (var _ = new SqliteCatalogReader(databasePath, "p0-json-import"))
        {
        }

        var dto = JsonSerializer.Deserialize<CatalogSensorsFileDto>(
            File.ReadAllText(jsonPath),
            JsonOptions) ?? throw new InvalidDataException("Catalog JSON deserialized to null.");

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        foreach (var sensor in dto.Sensors.OrderBy(s => s.PlatformId, StringComparer.Ordinal)
                     .ThenBy(s => s.SensorId, StringComparer.Ordinal))
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

    public static string ResolveBalticSensorsJsonPath() => ResolveRepoRelative(
        Path.Combine("assets", "data", "catalog", "sensors_baltic.json"));

    private static string ResolveRepoRelative(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return relativePath;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed class CatalogSensorsFileDto
    {
        [JsonPropertyName("sensors")]
        public List<CatalogSensorRowDto> Sensors { get; init; } = [];
    }

    private sealed class CatalogSensorRowDto
    {
        public string PlatformId { get; init; } = "";

        public string SensorId { get; init; } = "";

        public double BasePd { get; init; }

        public string SourceFactId { get; init; } = "catalog-import";

        public double Confidence { get; init; } = 1.0;
    }
}