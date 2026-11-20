using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactClassifyTests
{
    [Test]
    public void Contact_promotes_detected_classified_identified_on_sustained_detections()
    {
        var lifecycle = new ScenarioContactLifecycle(
            StaleThresholdTicks: 10,
            ClassifyAfterTicks: 1,
            IdentifyAfterTicks: 2);
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(7),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)],
            contactLifecycle: lifecycle);

        var t1 = sim.Tick(1, 1.0);
        Assert.That(t1.Any(t => t.NewState == ContactLifecycleState.Detected), Is.True);

        var t2 = sim.Tick(2, 2.0);
        Assert.That(t2.Any(t => t is { PreviousState: ContactLifecycleState.Detected, NewState: ContactLifecycleState.Classified }), Is.True);

        var t3 = sim.Tick(3, 3.0);
        Assert.That(t3.Any(t => t is { PreviousState: ContactLifecycleState.Classified, NewState: ContactLifecycleState.Identified }), Is.True);

        var t4 = sim.Tick(4, 4.0);
        Assert.That(t4.Any(t => t.NewState is ContactLifecycleState.Classified or ContactLifecycleState.Identified), Is.False);
    }

    [Test]
    public void Default_lifecycle_does_not_emit_classify_or_identify_transitions()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(8),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)],
            contactLifecycle: ScenarioContactLifecycle.Default);

        var all = new List<ContactTransition>();
        for (ulong tick = 1; tick <= 5; tick++)
        {
            all.AddRange(sim.Tick(tick, tick));
        }

        Assert.That(all.Any(t => t.NewState is ContactLifecycleState.Classified or ContactLifecycleState.Identified), Is.False);
    }
}