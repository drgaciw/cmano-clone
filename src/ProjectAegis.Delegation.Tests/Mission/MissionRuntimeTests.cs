namespace ProjectAegis.Delegation.Tests.Mission;

using ProjectAegis.Delegation.Mission;
using NUnit.Framework;

[TestFixture]
public sealed class MissionRuntimeTests
{
    [Test]
    public void Fire_order_same_tick_respects_locked_order()
    {
        var runtime = new MissionRuntime(
        [
            new MissionEventDefinition("b", 1, MissionEventKind.EventFired, "e2"),
            new MissionEventDefinition("a", 1, MissionEventKind.MissionTransition, "Execution"),
        ],
        ["a", "b"]);

        var emissions = runtime.Tick(1, 1.0, 0);
        Assert.That(emissions.Count, Is.EqualTo(2));
        Assert.That(emissions[0].Event.EventId, Is.EqualTo("a"));
        Assert.That(emissions[1].Event.EventId, Is.EqualTo("b"));
    }

    [Test]
    public void Events_do_not_refire_on_later_ticks()
    {
        var runtime = new MissionRuntime(
        [new MissionEventDefinition("start", 0, MissionEventKind.MissionTransition, "Planning")],
        ["start"]);

        Assert.That(runtime.Tick(0, 0, 0).Count, Is.EqualTo(1));
        Assert.That(runtime.Tick(5, 5, 0), Is.Empty);
    }
}