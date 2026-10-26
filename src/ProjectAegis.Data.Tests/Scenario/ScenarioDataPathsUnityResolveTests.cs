namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario;
using Xunit;

/// <summary>
/// Unity Editor BaseDirectory often sits under the Editor install or Library/ScriptAssemblies,
/// so walking only from AppContext.BaseDirectory can miss repo data/scenarios.
/// </summary>
public sealed class ScenarioDataPathsUnityResolveTests
{
    [Fact]
    public void TryResolveFrom_walks_up_from_unity_project_like_start_to_data_scenarios()
    {
        var repoRoot = ScenarioDataPaths.TryResolveRepoRoot();
        if (repoRoot == null)
        {
            return;
        }

        var unityProjectLike = Path.Combine(repoRoot, "unity", "ProjectAegis", "Library", "ScriptAssemblies");
        Directory.CreateDirectory(unityProjectLike);

        var resolved = ScenarioDataPaths.TryResolveFrom(unityProjectLike);
        Assert.NotNull(resolved);
        Assert.True(Directory.Exists(resolved));
        Assert.Contains(
            Directory.EnumerateFiles(resolved!, "*.policy.json"),
            f => Path.GetFileName(f).Equals("baltic-patrol-classify.policy.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryResolveScenariosDirectory_falls_back_to_current_directory_walk()
    {
        var repoRoot = ScenarioDataPaths.TryResolveRepoRoot();
        if (repoRoot == null)
        {
            return;
        }

        var previous = Directory.GetCurrentDirectory();
        var unityProject = Path.Combine(repoRoot, "unity", "ProjectAegis");
        if (!Directory.Exists(unityProject))
        {
            return;
        }

        try
        {
            Directory.SetCurrentDirectory(unityProject);
            var resolved = ScenarioDataPaths.TryResolveScenariosDirectory();
            Assert.NotNull(resolved);
            Assert.True(File.Exists(Path.Combine(resolved!, "baltic-patrol-classify.policy.json")));
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }
}
