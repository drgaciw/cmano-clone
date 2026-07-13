namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

/// <summary>Writes canonical scenario JSON (camelCase) for MCP mutating tools.</summary>
public static class ScenarioDocumentJsonWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    public static string Serialize(ScenarioDocumentDto document) =>
        JsonSerializer.Serialize(document, Options);

    public static void WriteToFile(ScenarioDocumentDto document, string path)
    {
        var json = Serialize(document);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, json);
    }
}