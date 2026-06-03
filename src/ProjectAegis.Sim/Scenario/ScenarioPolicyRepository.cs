namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Scenario;

public static class ScenarioPolicyRepository
{
    private static IReadOnlyDictionary<string, ScenarioPolicyProfile>? _jsonProfiles;
    private static string? _loadedFrom;

    public static void LoadFromDirectory(string directoryPath)
    {
        _jsonProfiles = ScenarioPolicyJsonLoader.LoadDirectory(directoryPath);
        _loadedFrom = directoryPath;
    }

    public static IReadOnlyList<string> AllIds()
    {
        EnsureDefaultJsonLoaded();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "baltic-patrol",
            "baltic-patrol-opp-hold-fire",
            "restricted-engagement",
            "test-sandbox-dual-side",
        };
        if (_jsonProfiles != null)
        {
            foreach (var key in _jsonProfiles.Keys)
            {
                ids.Add(key);
            }
        }

        return ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    public static ScenarioPolicyProfile? TryGet(string scenarioId)
    {
        EnsureDefaultJsonLoaded();

        if (_jsonProfiles != null &&
            _jsonProfiles.TryGetValue(scenarioId, out var fromJson))
        {
            return fromJson;
        }

        return ScenarioPolicyCatalog.TryGetBuiltIn(scenarioId);
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
            _jsonProfiles = new Dictionary<string, ScenarioPolicyProfile>();
            return;
        }

        LoadFromDirectory(dir);
    }
}
