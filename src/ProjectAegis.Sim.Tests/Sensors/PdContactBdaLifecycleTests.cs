using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactBdaLifecycleTests
{
    [Test]
    public void Bda_lost_target_does_not_reappear_on_later_ticks()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)]);

        sim.Tick(1, 1.0);
        sim.ApplyTargetBdaLost(1, 1.0, "hostile-1");
        Assert.That(sim.ActiveCount, Is.EqualTo(0));
        Assert.That(sim.IsTargetDestroyed("hostile-1"), Is.False);

        for (var t = 2; t <= 4; t++)
        {
            var transitions = sim.Tick((ulong)t, t);
            Assert.That(transitions.Any(tr => tr.NewState == ContactLifecycleState.Detected), Is.False);
            Assert.That(sim.ActiveCount, Is.EqualTo(0));
        }
    }

    [Test]
    public void Bda_lost_does_not_emit_transition_when_contact_already_lost()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)]);

        sim.Tick(1, 1.0);
        sim.ApplyTargetBdaLost(1, 1.0, "hostile-1");

        var repeat = sim.ApplyTargetBdaLost(2, 2.0, "hostile-1");

        Assert.That(repeat, Is.Empty);
    }
}