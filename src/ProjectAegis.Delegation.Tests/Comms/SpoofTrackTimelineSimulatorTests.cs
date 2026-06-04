using ProjectAegis.Delegation.Comms;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Comms;

[TestFixture]
public sealed class SpoofTrackTimelineSimulatorTests
{
    [Test]
    public void Advance_activates_spoof_at_configured_tick()
    {
        var sim = new SpoofTrackTimelineSimulator(
        [
            new ScenarioSpoofTransition(2, "hostile-1", "spoof"),
        ]);

        sim.Advance(1);
        Assert.That(sim.IsSpoofed("hostile-1"), Is.False);

        sim.Advance(2);
        Assert.That(sim.IsSpoofed("hostile-1"), Is.True);
    }
}