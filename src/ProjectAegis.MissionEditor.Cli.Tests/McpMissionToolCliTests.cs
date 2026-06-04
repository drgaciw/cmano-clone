using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class McpMissionToolCliTests
{
    [Fact]
    public void scenario_create_patrol_strike_validate_sample_pipeline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, writer));
                Assert.Contains("\"ok\":true", writer.ToString());
            }

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: false, writer));
                Assert.Contains("worldHash", writer.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void mission_add_patrol_stale_edit_version_returns_conflict_exit_code()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mcp-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            using (var writer = new StringWriter())
            {
                Assert.Equal(3, MissionAddPatrolCommand.Run(path, 99, "patrol-1", ["u1"], zone, writer));
                Assert.Contains("CONFLICT", writer.ToString());
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}