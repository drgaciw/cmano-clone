namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class MissionListBridgeTests
{
    [Test]
    public void Mission_scenario_projects_timeline_events()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-mission", ticks: 1);
        _ = result;
        ProjectAegis.Sim.Scenario.ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ProjectAegis.Sim.Scenario.ScenarioPolicyRepository.TryGet("baltic-patrol-mission");
        Assert.That(profile?.MissionTimeline, Is.Not.Null);
        var missions = MissionListBridge.ProjectFrom(profile!.MissionTimeline);
        Assert.That(missions.Any(m => m.EventId == "start-exec"), Is.True);
    }
}