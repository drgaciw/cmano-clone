using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class SwarmDetectionLoopIntegrationTests
{
    [Test]
    public void RollTick_uses_swarm_integrity_scale_from_trial()
    {
        var seed = SimSeed.FromScenario(11);
        const ulong tick = 3;
        var fullTrial = new ScenarioDetectionTrial(
            "swarm-obs",
            "eo",
            "tgt",
            "c1",
            BasePd: 0.8,
            RequiresActiveRadar: false,
            SwarmIntegrityScale: SwarmSensorScale.ForTrial(40, 40));
        var halfTrial = fullTrial with
        {
            SwarmIntegrityScale = SwarmSensorScale.ForTrial(20, 40),
        };

        var full = DeterministicDetectionLoop.RollTick(seed, tick, [fullTrial], unitRadarEmcon: null);
        var half = DeterministicDetectionLoop.RollTick(seed, tick, [halfTrial], unitRadarEmcon: null);

        AssertEx.Single(full);
        AssertEx.Single(half);
        Assert.That(
            Math.Round(full[0].Pd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.8, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(half[0].Pd, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.4, 6, MidpointRounding.AwayFromZero)));
        Assert.That(half[0].Pd < full[0].Pd, Is.True);
        // Same draw stream keying: observer/sensor/target identical ⇒ same draw
        Assert.That(half[0].Draw, Is.EqualTo(full[0].Draw));
        if (full[0].Draw is > 0.4 and < 0.8)
        {
            Assert.That(full[0].Detected, Is.True);
            Assert.That(half[0].Detected, Is.False);
        }
    }

    [Test]
    public void ScaleFactor_accepts_scenario_power_override()
    {
        var linear = SwarmSensorScale.ScaleFactor(10, 40, integrityPower: 1.0);
        var squared = SwarmSensorScale.ScaleFactor(10, 40, integrityPower: 2.0);
        Assert.That(
            Math.Round(linear, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.25, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(squared, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.0625, 6, MidpointRounding.AwayFromZero)));
        Assert.That(squared < linear, Is.True);
    }
}
