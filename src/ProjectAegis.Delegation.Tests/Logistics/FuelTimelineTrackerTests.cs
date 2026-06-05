using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
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

        var bands = new List<FuelStateChangeRecord>();
        for (var t = 1; t <= 113; t++)
        {
            bands.AddRange(tracker.Drain((ulong)t, t, 1.0, [unit]).BandChanges);
        }

        Assert.That(bands.Any(b => b.NewState == "JOKER"), Is.True);
        Assert.That(bands.Any(b => b.NewState == "BINGO"), Is.True);
    }

    [Test]
    public void Drain_emits_tick_burn_rows_when_logTickBurn_enabled()
    {
        var logistics = new ScenarioLogisticsSettings(
            300,
            600,
            fuelCapacityKg: 10_000,
            burnRateKgPerSecond: 80,
            logTickBurn: true);
        var tracker = new FuelTimelineTracker(logistics);
        var unit = new TargetId("u1");

        var result = tracker.Drain(1, 1, 1.0, [unit]);
        Assert.That(result.Burns, Has.Count.EqualTo(1));
        Assert.That(result.Burns[0].DeltaKg, Is.EqualTo(-80).Within(0.001));
    }

    [Test]
    public void TryCreate_returns_null_without_burn_model()
    {
        var profile = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree, logistics: new ScenarioLogisticsSettings(50, 100));
        Assert.That(FuelTimelineTracker.TryCreate(profile), Is.Null);
    }
}