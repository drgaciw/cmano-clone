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

        // ADR-022 Decision 3: EnumerateFiles returns filesystem order — roughly
        // alphabetical on NTFS, inode order on ext4 — so without an explicit sort
        // the host OS would decide which of two files sharing an Id survives.
        var files = Directory.EnumerateFiles(directoryPath, "*.policy.json")
            .OrderBy(f => f, StringComparer.Ordinal);

        // Same comparer as the map: ids differing only by case collide there too.
        var sourceById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(json, Options)
                ?? throw new InvalidDataException($"Invalid scenario policy JSON: {file}");
            if (sourceById.TryGetValue(dto.Id, out var firstFile))
            {
                throw new InvalidDataException(
                    $"Duplicate scenario policy id '{dto.Id}' declared in '{firstFile}' and '{file}'.");
            }

            sourceById[dto.Id] = file;
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