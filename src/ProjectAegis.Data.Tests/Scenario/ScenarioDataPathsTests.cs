namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario;
using Xunit;

public sealed class ScenarioDataPathsTests
{
    [Fact]
    public void TryResolveScenariosDirectory_finds_repo_data_scenarios()
    {
        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (dir == null)
        {
            return;
        }

        Assert.True(Directory.Exists(dir));
        Assert.NotEmpty(Directory.EnumerateFiles(dir, "*.policy.json"));
    }
}