using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class ScenarioCommsStatusCommandTests
{
    [Fact]
    public void Run_returns_comms_timeline_for_baltic_comms_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCommsStatusCommand.Run("baltic-patrol-comms", writer);
        var json = writer.ToString();

        Assert.Equal(0, exit);
        Assert.Contains("\"ok\": true", json);
        Assert.Contains("degradedOrderDelayTicks", json);
        Assert.Contains("Degraded", json);
    }

    [Fact]
    public void Run_returns_error_for_unknown_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCommsStatusCommand.Run("missing-policy", writer);

        Assert.Equal(1, exit);
        Assert.Contains("policy_not_found", writer.ToString());
    }
}