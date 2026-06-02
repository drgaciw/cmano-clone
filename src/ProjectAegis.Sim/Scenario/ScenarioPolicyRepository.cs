namespace ProjectAegis.Sim.Scenario;

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

        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            _jsonProfiles = new Dictionary<string, ScenarioPolicyProfile>();
            return;
        }

        var dir = Path.Combine(repoRoot, "data", "scenarios");
        LoadFromDirectory(dir);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "data", "scenarios")))
            {
                return dir;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return null;
    }
}
