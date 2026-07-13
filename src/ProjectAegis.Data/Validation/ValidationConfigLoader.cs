namespace ProjectAegis.Data.Validation;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Loads GDD §7 tuning knobs from assets/data/editor/validation-config.json.</summary>
public static class ValidationConfigLoader
{
    public static ValidationConfig LoadFromRepo(string? repoRoot = null)
    {
        repoRoot ??= FindRepoRoot();
        var path = Path.Combine(repoRoot, "assets", "data", "editor", "validation-config.json");
        if (!File.Exists(path))
        {
            return new ValidationConfig();
        }

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<ValidationConfigFileDto>(json, JsonOptions)
            ?? throw new InvalidDataException($"Invalid validation config: {path}");

        return new ValidationConfig(
            dto.IngressEgressPadNm,
            dto.FuelFraction,
            ParseSeverityFloor(dto.ExportBlockSeverityFloor),
            dto.ComplexityWarnThreshold,
            dto.DensityWarnThreshold,
            dto.CrossRefWeight,
            dto.MaxConditionsPerEvent);
    }

    private static ValidationSeverity ParseSeverityFloor(string? value) =>
        string.Equals(value, "warning", StringComparison.OrdinalIgnoreCase)
            ? ValidationSeverity.Warning
            : ValidationSeverity.Error;

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        return Directory.GetCurrentDirectory();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private sealed class ValidationConfigFileDto
    {
        public double IngressEgressPadNm { get; init; } = 50;

        public double FuelFraction { get; init; } = 0.85;

        public string ExportBlockSeverityFloor { get; init; } = "error";

        public int ComplexityWarnThreshold { get; init; } = 400;

        public int DensityWarnThreshold { get; init; } = 20;

        public int CrossRefWeight { get; init; } = 2;

        public int MaxConditionsPerEvent { get; init; } = 32;
    }
}
