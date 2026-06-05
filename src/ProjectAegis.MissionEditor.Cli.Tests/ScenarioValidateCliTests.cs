namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public sealed class ScenarioValidateCliTests
{
    [Fact]
    public void scenario_validate_clean_fixture_returns_exit_0()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
        Assert.Contains("passed", writer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void scenario_validate_unreachable_fixture_returns_exit_1()
    {
        var path = ResolveFixture("golden_strike_unreachable.json");
        if (path == null)
        {
            return;
        }

        using var writer = new StringWriter();
        Assert.Equal(1, ScenarioValidateCommand.Run(path, quiet: false, writer));
        Assert.Contains("STRIKE_UNREACHABLE", writer.ToString());
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