using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyJsonLoaderTests
{
    [Fact]
    public void Loads_restricted_engagement_with_unit_override()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "restricted-engagement.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("restricted-engagement", profile.Id);
        Assert.Equal(RoeLevel.WeaponsTight, profile.ResolveForUnit("o1", isFriendly: false).Roe);
        Assert.Equal(RoeLevel.WeaponsFree, profile.ResolveForUnit("f1", isFriendly: true).Roe);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
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
