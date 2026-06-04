namespace ProjectAegis.Data.Scenario.Policy;

using System.Text.Json;

/// <summary>Loads and caches scenario policy JSON from <c>data/scenarios</c> (DATA-3 persistence seam).</summary>
public static class ScenarioPolicyJsonIndex
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static IReadOnlyDictionary<string, ScenarioPolicyJsonDto>? _jsonProfiles;
    private static string? _loadedFrom;

    public static void LoadFromDirectory(string directoryPath)
    {
        var map = new Dictionary<string, ScenarioPolicyJsonDto>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directoryPath))
        {
            _jsonProfiles = map;
            _loadedFrom = directoryPath;
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.policy.json"))
        {
            var json = File.ReadAllText(file);
            var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options)
                ?? throw new InvalidDataException($"Invalid scenario policy JSON: {file}");
            map[dto.Id] = dto;
        }

        _jsonProfiles = map;
        _loadedFrom = directoryPath;
    }

    public static void EnsureDefaultJsonLoaded()
    {
        if (_jsonProfiles != null)
        {
            return;
        }

        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (dir == null)
        {
            _jsonProfiles = new Dictionary<string, ScenarioPolicyJsonDto>();
            return;
        }

        LoadFromDirectory(dir);
    }

    public static IReadOnlyList<string> AllJsonPolicyIds()
    {
        EnsureDefaultJsonLoaded();
        return _jsonProfiles!.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    public static ScenarioPolicyJsonDto? TryGetJson(string scenarioId)
    {
        EnsureDefaultJsonLoaded();
        if (_jsonProfiles!.TryGetValue(scenarioId, out var fromJson))
        {
            return fromJson;
        }

        return null;
    }

    public static string? LoadedFromDirectory => _loadedFrom;
}