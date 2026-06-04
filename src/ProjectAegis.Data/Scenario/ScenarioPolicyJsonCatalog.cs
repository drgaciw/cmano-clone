namespace ProjectAegis.Data.Scenario;

using ProjectAegis.Data.Scenario.Policy;

/// <summary>Scenario policy JSON catalog in the Data layer (profiles resolved in Sim via <c>ScenarioPolicyJsonLoader</c>).</summary>
public static class ScenarioPolicyJsonCatalog
{
    private static readonly string[] BuiltInIds =
    [
        "baltic-patrol",
        "baltic-patrol-opp-hold-fire",
        "restricted-engagement",
        "test-sandbox-dual-side",
    ];

    public static void LoadFromDirectory(string directoryPath) =>
        ScenarioPolicyJsonIndex.LoadFromDirectory(directoryPath);

    public static void EnsureDefaultJsonLoaded() =>
        ScenarioPolicyJsonIndex.EnsureDefaultJsonLoaded();

    public static IReadOnlyList<string> AllIds()
    {
        EnsureDefaultJsonLoaded();
        var ids = new HashSet<string>(BuiltInIds, StringComparer.OrdinalIgnoreCase);
        foreach (var key in ScenarioPolicyJsonIndex.AllJsonPolicyIds())
        {
            ids.Add(key);
        }

        return ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    public static ScenarioPolicyJsonDto? TryGetJson(string scenarioId) =>
        ScenarioPolicyJsonIndex.TryGetJson(scenarioId);
}