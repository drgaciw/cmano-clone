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
    public void TryResolveScenariosDirectory_falls_back_to_working_directory_when_base_is_outside_repo()
    {
        // Unity Editor: AppContext.BaseDirectory is the Editor install (outside the repo), while the
        // working directory is unity/ProjectAegis. The resolver must still find <repo>/data/scenarios.
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var outsideBase = Path.Combine(Path.GetTempPath(), "aegis-scenario-base-" + Guid.NewGuid().ToString("N"));
        var workingDir = Path.Combine(repoRoot!, "unity", "ProjectAegis");

        var dir = ScenarioDataPaths.TryResolveScenariosDirectory(outsideBase, workingDir);

        Assert.That(dir, Is.EqualTo(Path.Combine(repoRoot!, "data", "scenarios")));
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

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
