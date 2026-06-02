using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactStaleTests
{
    [Fact]
    public void Contact_goes_lost_after_stale_threshold_missed_ticks()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(5),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)],
            contactLifecycle: new ScenarioContactLifecycle(StaleThresholdTicks: 1));
        var t1 = sim.Tick(1, 1.0);
        Assert.Contains(t1, t => t.NewState == ContactLifecycleState.Detected);
        Assert.Equal(1, sim.ActiveCount);
        var t2 = sim.Tick(2, 2.0);
        Assert.Contains(t2, t => t.NewState == ContactLifecycleState.Lost);
        Assert.Equal(0, sim.ActiveCount);
    }
}