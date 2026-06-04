using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class ScenarioCyberStatusCommandTests
{
    [Fact]
    public void Run_returns_cyber_codes_for_baltic_comms_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCyberStatusCommand.Run("baltic-patrol-comms", writer);
        var json = writer.ToString();

        Assert.Equal(0, exit);
        Assert.Contains("CYBER_LINK_DOWN", json);
        Assert.Contains("commsOrderDelayTicks", json);
    }
}