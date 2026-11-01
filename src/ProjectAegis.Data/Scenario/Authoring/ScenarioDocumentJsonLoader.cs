namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

public static class ScenarioDocumentJsonLoader
{
    private static readonly JsonSerializerOptions Options = CreateOptions(strict: false);

    // DRG-694: opt-in strict mode rejects duplicate JSON properties (System.Text.Json 9+).
    private static readonly JsonSerializerOptions StrictOptions = CreateOptions(strict: true);

    public static ScenarioDocumentDto LoadFromFile(string path) => LoadFromFile(path, strict: false);

    /// <summary>Loads a scenario document. When <paramref name="strict"/> is true, duplicate
    /// properties (including case-insensitive duplicates) throw <see cref="InvalidDataException"/>.</summary>
    public static ScenarioDocumentDto LoadFromFile(string path, bool strict)
    {
        var json = File.ReadAllText(path);
        try
        {
            return JsonSerializer.Deserialize<ScenarioDocumentDto>(json, strict ? StrictOptions : Options)
                ?? throw new InvalidDataException($"Invalid scenario document: {path}");
        }
        catch (JsonException ex) when (strict)
        {
            throw new InvalidDataException($"Invalid scenario document (strict): {path}: {ex.Message}", ex);
        }
    }

    private static JsonSerializerOptions CreateOptions(bool strict) => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowDuplicateProperties = !strict,
    };
}
