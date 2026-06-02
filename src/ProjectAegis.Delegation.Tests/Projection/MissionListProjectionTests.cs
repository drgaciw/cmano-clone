using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MissionListProjectionTests
{
    [Test]
    public void Project_orders_events_by_tick_then_id()
    {
        var timeline = new ScenarioMissionTimeline(
            ["b", "a"],
            [
                new ScenarioMissionEvent("b", 2, "EventFired", "late"),
                new ScenarioMissionEvent("a", 1, "MissionTransition", "early"),
            ]);

        var rows = MissionListProjection.Project(timeline);

        Assert.That(rows[0].EventId, Is.EqualTo("a"));
        Assert.That(rows[1].EventId, Is.EqualTo("b"));
    }

    [Test]
    public void Project_null_timeline_returns_empty()
    {
        Assert.That(MissionListProjection.Project(null), Is.Empty);
    }
}