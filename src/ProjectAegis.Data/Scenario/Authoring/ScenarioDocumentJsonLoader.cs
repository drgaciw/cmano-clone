namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

public static class ScenarioDocumentJsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ScenarioDocumentDto LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ScenarioDocumentDto>(json, Options)
            ?? throw new InvalidDataException($"Invalid scenario document: {path}");
    }
}