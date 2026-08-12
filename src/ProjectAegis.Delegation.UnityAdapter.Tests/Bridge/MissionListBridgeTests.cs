namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System.Linq;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless dogfood for mission list presentation bridge (UCA-P1c / DRG-146).
/// Null timeline is a valid empty picture (projection contract).
/// </summary>
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

    [Test]
    public void ProjectFrom_null_timeline_returns_empty_readonly_list()
    {
        var missions = MissionListBridge.ProjectFrom(null);
        Assert.That(missions, Is.InstanceOf<System.Collections.Generic.IReadOnlyList<MissionListEntry>>());
        Assert.That(missions, Is.Empty);
    }
}
