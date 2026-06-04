using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicySpoofReadinessJsonTests
{
    [Fact]
    public void Load_spoof_and_readiness_policies_from_data_directory()
    {
        var dir = ResolveScenariosDir();
        var spoof = ScenarioPolicyJsonLoader.LoadFromFile(Path.Combine(dir, "baltic-patrol-spoof.policy.json"));
        var readiness = ScenarioPolicyJsonLoader.LoadFromFile(Path.Combine(dir, "baltic-patrol-readiness.policy.json"));

        Assert.Single(spoof.SpoofTransitions);
        Assert.Equal("hostile-1", spoof.SpoofTransitions[0].ContactId);
        Assert.False(readiness.UnitReadiness["u1"]);
    }

    private static string ResolveScenariosDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "scenarios");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new DirectoryNotFoundException("data/scenarios");
    }
}