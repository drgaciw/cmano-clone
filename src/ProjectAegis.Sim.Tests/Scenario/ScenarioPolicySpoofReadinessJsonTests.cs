using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicySpoofReadinessJsonTests
{
    [Test]
    public void Load_spoof_and_readiness_policies_from_data_directory()
    {
        var dir = ResolveScenariosDir();
        var spoof = ScenarioPolicyJsonLoader.LoadFromFile(Path.Combine(dir, "baltic-patrol-spoof.policy.json"));
        var readiness = ScenarioPolicyJsonLoader.LoadFromFile(Path.Combine(dir, "baltic-patrol-readiness.policy.json"));

        AssertEx.Single(spoof.SpoofTransitions);
        Assert.That(spoof.SpoofTransitions[0].ContactId, Is.EqualTo("hostile-1"));
        Assert.That(readiness.UnitReadiness["u1"], Is.False);
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