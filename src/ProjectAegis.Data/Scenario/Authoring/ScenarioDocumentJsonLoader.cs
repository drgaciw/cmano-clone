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

    // DRG-694: opt-in strict mode rejects duplicate JSON properties (System.Text.Json 10+).
    private static readonly JsonSerializerOptions StrictOptions = new(Options)
    {
        AllowDuplicateProperties = false,
    };

    public static ScenarioDocumentDto LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ScenarioDocumentDto>(json, Options)
            ?? throw new InvalidDataException($"Invalid scenario document: {path}");
    }

    /// <summary>Loads a scenario document. When <paramref name="strict"/> is true, duplicate
    /// properties, including unmapped properties and case-insensitive DTO member duplicates,
    /// throw <see cref="InvalidDataException"/>. Dictionary keys remain case-sensitive.</summary>
    public static ScenarioDocumentDto LoadFromFile(string path, bool strict)
    {
        if (!strict)
            return LoadFromFile(path);

        var json = File.ReadAllText(path);
        try
        {
            return DeserializeStrict(json)
                ?? throw new InvalidDataException($"Invalid scenario document: {path}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid scenario document (strict): {path}: {ex.Message}", ex);
        }
    }

    private static ScenarioDocumentDto? DeserializeStrict(string json)
    {
        // The serializer skips unmapped values without checking their property names.
        // Validate the whole document so duplicate names cannot hide in skipped content.
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            AllowDuplicateProperties = false,
        });
        return document.RootElement.Deserialize<ScenarioDocumentDto>(StrictOptions);
    }

}
