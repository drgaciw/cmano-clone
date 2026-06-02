using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Comms;

public sealed class CommsTimelineSimulatorTests
{
    [Test]
    public void Drain_emits_transitions_at_configured_ticks()
    {
        var sim = new CommsTimelineSimulator(
        [
            new ScenarioCommsTransition(2, "Degraded", "net-1", "jam"),
            new ScenarioCommsTransition(4, "Denied", "net-1", "down"),
        ]);

        Assert.That(sim.Drain(1, 1.0), Is.Empty);
        var step2 = sim.Drain(2, 2.0);
        Assert.That(step2, Has.Count.EqualTo(1));
        Assert.That(step2[0].NewState, Is.EqualTo(CommsState.Degraded));
        Assert.That(sim.CurrentState, Is.EqualTo(CommsState.Degraded));

        var step4 = sim.Drain(4, 4.0);
        Assert.That(step4[0].NewState, Is.EqualTo(CommsState.Denied));
    }
}