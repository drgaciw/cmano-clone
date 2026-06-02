using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactKillTests
{
    [Fact]
    public void Destroyed_target_does_not_reappear_on_later_ticks()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)]);

        sim.Tick(1, 1.0);
        sim.ApplyTargetKill(1, 1.0, "hostile-1");

        for (var t = 2; t <= 4; t++)
        {
            var transitions = sim.Tick((ulong)t, t);
            Assert.DoesNotContain(transitions, tr => tr.NewState == ContactLifecycleState.Detected);
            Assert.Equal(0, sim.ActiveCount);
        }

        Assert.True(sim.IsTargetDestroyed("hostile-1"));
    }
}