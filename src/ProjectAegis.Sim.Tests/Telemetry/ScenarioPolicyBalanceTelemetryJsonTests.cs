using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Telemetry;

public sealed class ScenarioPolicyBalanceTelemetryJsonTests
{
    [Test]
    public void Balance_telemetry_defaults_to_disabled_when_omitted()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.That(profile.BalanceTelemetry.EnableBalanceDrift, Is.False);
    }

    [Test]
    public void Balance_telemetry_fixture_enables_drift_with_fast_thresholds()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-balance-drift.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);

        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-balance-drift"));
        Assert.That(profile.BalanceTelemetry.EnableBalanceDrift, Is.True);
        Assert.That(profile.BalanceTelemetry.Options!.MinimumSampleRuns, Is.EqualTo(100));
        Assert.That(profile.BalanceTelemetry.Options.WinRateDriftThreshold, Is.EqualTo(0.08));
        AssertEx.Single(profile.BalanceTelemetry.BalanceTrials);
        Assert.That(profile.BalanceTelemetry.BalanceTrials[0].EntityId, Is.EqualTo("u1"));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "data", "scenarios")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}