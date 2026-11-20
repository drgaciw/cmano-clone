using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class ScenarioContactEmconTests
{
    [Test]
    public void Radar_off_skips_active_radar_contact_seed()
    {
        var emcon = new Dictionary<string, EmconState> { ["u1"] = EmconState.Off };
        var sim = new ScenarioContactSimulator(
        [
            new ScenarioContactSeed("u1", "hostile-1", "c1", 1),
        ],
            emcon);
        var transitions = sim.Tick(1, 1.0);
        Assert.That(transitions, Is.Empty);
        Assert.That(sim.ActiveCount, Is.EqualTo(0));
    }
}