using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class ScenarioCommsStatusCommandTests
{
    [Test]
    public void Run_returns_comms_timeline_for_baltic_comms_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCommsStatusCommand.Run("baltic-patrol-comms", writer);
        var json = writer.ToString();

        Assert.That(exit, Is.EqualTo(0));
        Assert.That(json, Does.Contain("\"ok\": true"));
        Assert.That(json, Does.Contain("degradedOrderDelayTicks"));
        Assert.That(json, Does.Contain("Degraded"));
    }

    [Test]
    public void Run_returns_error_for_unknown_policy()
    {
        using var writer = new StringWriter();
        var exit = ScenarioCommsStatusCommand.Run("missing-policy", writer);

        Assert.That(exit, Is.EqualTo(1));
        Assert.That(writer.ToString(), Does.Contain("policy_not_found"));
    }
}