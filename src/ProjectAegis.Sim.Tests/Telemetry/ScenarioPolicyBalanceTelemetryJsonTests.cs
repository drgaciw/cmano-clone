using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Telemetry;

public sealed class ScenarioPolicyBalanceTelemetryJsonTests
{
    [Fact]
    public void Balance_telemetry_defaults_to_disabled_when_omitted()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.False(profile.BalanceTelemetry.EnableBalanceDrift);
    }

    [Fact]
    public void Balance_telemetry_fixture_enables_drift_with_fast_thresholds()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-balance-drift.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);

        Assert.Equal("baltic-patrol-balance-drift", profile.Id);
        Assert.True(profile.BalanceTelemetry.EnableBalanceDrift);
        Assert.Equal(100, profile.BalanceTelemetry.Options!.MinimumSampleRuns);
        Assert.Equal(0.08, profile.BalanceTelemetry.Options.WinRateDriftThreshold);
        Assert.Single(profile.BalanceTelemetry.BalanceTrials);
        Assert.Equal("u1", profile.BalanceTelemetry.BalanceTrials[0].EntityId);
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