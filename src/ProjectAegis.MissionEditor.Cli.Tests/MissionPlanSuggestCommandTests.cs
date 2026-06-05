using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class MissionPlanSuggestCommandTests
{
    [Fact]
    public void Run_includes_baltic_and_comms_suggestions()
    {
        using var writer = new StringWriter();
        var exit = MissionPlanSuggestCommand.Run("baltic patrol with comms degraded", writer);
        var json = writer.ToString();

        Assert.Equal(0, exit);
        Assert.Contains("scenario_create", json);
        Assert.Contains("mission_add_patrol", json);
        Assert.Contains("scenario_comms_status", json);
        Assert.Contains("baltic-patrol-comms", json);
    }
}