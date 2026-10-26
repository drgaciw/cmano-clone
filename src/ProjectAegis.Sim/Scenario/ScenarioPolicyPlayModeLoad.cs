namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Scenario;

/// <summary>
/// Loads scenario policy JSON for Play Mode hosts whose process directory is the
/// Unity Editor install, not the repo. Pass <c>Application.dataPath</c>.
/// </summary>
public static class ScenarioPolicyPlayModeLoad
{
    /// <summary>
    /// Walks ancestors of <paramref name="startPath"/> for <c>data/scenarios</c> and loads that directory.
    /// </summary>
    /// <returns>True when a scenarios directory was found and loaded.</returns>
    public static bool EnsureLoadedFrom(string? startPath)
    {
        var dir = ScenarioDataPaths.TryResolveFrom(startPath);
        if (string.IsNullOrEmpty(dir))
        {
            return false;
        }

        ScenarioPolicyRepository.LoadFromDirectory(dir);
        return true;
    }
}
