using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Logistics;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Logistics;

public sealed class FuelTimelineTrackerTests
{
    [Test]
    public void Drain_emits_joker_then_bingo_on_burn_model()
    {
        var logistics = new ScenarioLogisticsSettings(300, 600, fuelCapacityKg: 10_000, burnRateKgPerSecond: 80);
        var tracker = new FuelTimelineTracker(logistics);
        var unit = new TargetId("u1");

        Assert.That(tracker.Drain(0, 10, [unit]), Is.Empty);

        var atJoker = tracker.Drain(94, 94, [unit]);
        Assert.That(atJoker, Has.Count.EqualTo(1));
        Assert.That(atJoker[0].NewState, Is.EqualTo("JOKER"));

        var atBingo = tracker.Drain(113, 113, [unit]);
        Assert.That(atBingo, Has.Count.EqualTo(1));
        Assert.That(atBingo[0].NewState, Is.EqualTo("BINGO"));
    }

    [Test]
    public void TryCreate_returns_null_without_burn_model()
    {
        var profile = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree, logistics: new ScenarioLogisticsSettings(50, 100));
        Assert.That(FuelTimelineTracker.TryCreate(profile), Is.Null);
    }
}