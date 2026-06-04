namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Policy;

/// <summary>Resolves <see cref="ScenarioPolicyProfile"/> from Data-layer JSON index + built-ins.</summary>
public static class ScenarioPolicyRepository
{
    public static void LoadFromDirectory(string directoryPath) =>
        ScenarioPolicyJsonCatalog.LoadFromDirectory(directoryPath);

    public static IReadOnlyList<string> AllIds() =>
        ScenarioPolicyJsonCatalog.AllIds();

    public static void EnsureDefaultJsonLoaded() =>
        ScenarioPolicyJsonCatalog.EnsureDefaultJsonLoaded();

    public static ScenarioPolicyProfile? TryGet(string scenarioId)
    {
        EnsureDefaultJsonLoaded();
        var dto = ScenarioPolicyJsonCatalog.TryGetJson(scenarioId);
        if (dto != null)
        {
            return ScenarioPolicyJsonLoader.ToProfile(dto);
        }

        return ScenarioPolicyCatalog.TryGetBuiltIn(scenarioId);
    }
}
