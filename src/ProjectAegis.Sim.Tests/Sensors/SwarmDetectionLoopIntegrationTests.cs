using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class SwarmDetectionLoopIntegrationTests
{
    [Fact]
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

        Assert.Single(full);
        Assert.Single(half);
        Assert.Equal(0.8, full[0].Pd, 6);
        Assert.Equal(0.4, half[0].Pd, 6);
        Assert.True(half[0].Pd < full[0].Pd);
        // Same draw stream keying: observer/sensor/target identical ⇒ same draw
        Assert.Equal(full[0].Draw, half[0].Draw);
        if (full[0].Draw is > 0.4 and < 0.8)
        {
            Assert.True(full[0].Detected);
            Assert.False(half[0].Detected);
        }
    }

    [Fact]
    public void ScaleFactor_accepts_scenario_power_override()
    {
        var linear = SwarmSensorScale.ScaleFactor(10, 40, integrityPower: 1.0);
        var squared = SwarmSensorScale.ScaleFactor(10, 40, integrityPower: 2.0);
        Assert.Equal(0.25, linear, 6);
        Assert.Equal(0.0625, squared, 6);
        Assert.True(squared < linear);
    }
}
