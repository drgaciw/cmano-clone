namespace ProjectAegis.Delegation.Tests.Mission;

using ProjectAegis.Delegation.Mission;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

[TestFixture]
public sealed class MissionContactTriggerRuntimeTests
{
    [Test]
    public void Fires_once_on_unknown_to_detected_matching_observer_and_class()
    {
        var triggers = new[]
        {
            new ScenarioMissionContactTrigger(
                "blue-asuw",
                "ucav-blue",
                MissionContactTargetClass.Surface,
                MissionContactPolicySide.Friendly,
                "ASuW",
                RoeLevel.WeaponsFree,
                ["u1"]),
        };
        var runtime = new MissionContactTriggerRuntime(triggers);
        var transition = new ContactTransition(
            1,
            1.0,
            "ucav-blue",
            "c-1",
            "hostile-1",
            ContactLifecycleState.Unknown,
            ContactLifecycleState.Detected);

        var first = runtime.Evaluate(transition, 1.0, 1);
        var second = runtime.Evaluate(transition, 1.0, 1);

        Assert.That(first, Has.Count.EqualTo(1));
        Assert.That(first[0].Trigger.MissionCode, Is.EqualTo("ASuW"));
        Assert.That(second, Is.Empty);
    }

    [Test]
    public void Ignores_air_trigger_for_surface_contact()
    {
        var triggers = new[]
        {
            new ScenarioMissionContactTrigger(
                "blue-aaa",
                "ucav-blue",
                MissionContactTargetClass.Air,
                MissionContactPolicySide.Friendly,
                "AAA",
                RoeLevel.WeaponsFree,
                ["u1"]),
        };
        var runtime = new MissionContactTriggerRuntime(triggers);
        var transition = new ContactTransition(
            1,
            1.0,
            "ucav-blue",
            "c-1",
            "hostile-1",
            ContactLifecycleState.Unknown,
            ContactLifecycleState.Detected);

        Assert.That(runtime.Evaluate(transition, 1.0, 1), Is.Empty);
    }
}
