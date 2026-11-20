using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class ScenarioCyberStatusCommandTests
{
    [Test]
    public void Run_returns_cyber_codes_for_baltic_comms_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCyberStatusCommand.Run("baltic-patrol-comms", writer);
        var json = writer.ToString();

        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("CYBER_LINK_DOWN"));
        Assert.That(json, Does.Contain("commsOrderDelayTicks"));
    }
}