namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public sealed class ScenarioSimulateSampleCliTests
{
    [Fact]
    public void scenario_simulate_sample_clean_fixture_returns_exit_0_with_world_hash()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 16, quiet: false, writer));
        var json = writer.ToString();
        Assert.Contains("worldHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fingerprint", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void scenario_simulate_sample_golden_clean_32_ticks_matches_pinned_world_hash()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, SimulateSampleGoldenHashes.GoldenTicks, quiet: false, writer));
        Assert.Contains(SimulateSampleGoldenHashes.GoldenCleanWorldHash, writer.ToString());
        Assert.Contains($"\"engagementCount\": {SimulateSampleGoldenHashes.GoldenCleanEngagementCount}", writer.ToString());
        Assert.Contains(SimulateSampleGoldenHashes.GoldenCleanDetectionWorldHash, writer.ToString());
    }

    [Fact]
    public void scenario_simulate_sample_unreachable_fixture_blocked_by_validation()
    {
        var path = ResolveFixture("golden_strike_unreachable.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: true, writer));
    }

    private static string? ResolveFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}