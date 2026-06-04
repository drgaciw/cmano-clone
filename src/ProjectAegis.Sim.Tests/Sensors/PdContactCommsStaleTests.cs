using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactCommsStaleTests
{
    [Fact]
    public void Degraded_comms_divisor_accelerates_contact_lost_transition()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0),
        };
        var lifecycle = new ScenarioContactLifecycle(StaleThresholdTicks: 4);
        var sim = new PdDetectionContactSimulator(SimSeed.FromScenario(9), trials, contactLifecycle: lifecycle);
        sim.Tick(1, 1.0);
        sim.SetCommsStaleThresholdDivisor(4);
        var lost = sim.Tick(2, 2.0).Any(t => t.NewState == ContactLifecycleState.Lost);
        Assert.True(lost);
    }
}