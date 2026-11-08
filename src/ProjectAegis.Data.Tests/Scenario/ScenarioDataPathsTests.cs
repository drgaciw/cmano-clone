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

    [Fact]
    public void TryResolveCampaignsDirectory_finds_repo_data_campaigns_when_present()
    {
        var dir = ScenarioDataPaths.TryResolveCampaignsDirectory();
        if (dir == null)
        {
            return;
        }

        Assert.True(Directory.Exists(dir));
        Assert.NotEmpty(Directory.EnumerateFiles(dir, "*.campaign.json"));
        Assert.Contains(
            Directory.EnumerateFiles(dir, "*.campaign.json"),
            f => Path.GetFileName(f).Equals("baltic-patrol-campaign.campaign.json", StringComparison.OrdinalIgnoreCase));
    }
}
