namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario;
using NUnit.Framework;

public sealed class ScenarioDataPathsTests
{
    [Test]
    public void TryResolveScenariosDirectory_finds_repo_data_scenarios()
    {
        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (dir == null)
        {
            return;
        }

        Assert.That(Directory.Exists(dir), Is.True);
        Assert.That(Directory.EnumerateFiles(dir, "*.policy.json"), Is.Not.Empty);
    }

    [Test]
    public void TryResolveCampaignsDirectory_finds_repo_data_campaigns_when_present()
    {
        var dir = ScenarioDataPaths.TryResolveCampaignsDirectory();
        if (dir == null)
        {
            return;
        }

        Assert.That(Directory.Exists(dir), Is.True);
        Assert.That(Directory.EnumerateFiles(dir, "*.campaign.json"), Is.Not.Empty);
        Assert.That(            Directory.EnumerateFiles(dir, "*.campaign.json").Any(            f => Path.GetFileName(f).Equals("baltic-patrol-campaign.campaign.json", StringComparison.OrdinalIgnoreCase)), Is.True);
    }
}
