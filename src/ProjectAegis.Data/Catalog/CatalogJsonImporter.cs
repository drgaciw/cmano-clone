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

        var batchId = dto.ImportBatchId ?? Path.GetFileNameWithoutExtension(jsonPath);
        var sourceFile = Path.GetFileName(jsonPath);
        return dto.Sensors
            .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
            .ThenBy(s => s.SensorId, StringComparer.Ordinal)
            .Select(s => new CatalogSensorBinding(
                s.PlatformId,
                s.SensorId,
                s.BasePd,
                s.SourceFactId,
                s.Confidence,
                batchId,
                sourceFile,
                NormalizeReviewState(s.ReviewState),
                Math.Clamp(s.TrlLevel <= 0 ? 9 : s.TrlLevel, 1, 9)))
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

        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        connection.Open();
        foreach (var sensor in bindings)
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

        SqliteConnection.ClearAllPools();
    }

    public static void ImportToSqlite(string jsonPath, string databasePath, bool overwrite = true) =>
        WriteSqlite(databasePath, CatalogImportGate.ApplyAllGates(ReadSensorBindings(jsonPath)), overwrite);

    private static string NormalizeReviewState(string? state) =>
        string.IsNullOrWhiteSpace(state)
            ? CatalogReviewStates.Approved
            : state.Trim().ToLowerInvariant();

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
        [JsonPropertyName("importBatchId")]
        public string? ImportBatchId { get; init; }

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

        public string? ReviewState { get; init; }

        public int TrlLevel { get; init; } = 9;
    }
}