using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdContactPrimaryBlueForceStaleTests
{
    [Fact]
    public void Stale_loss_of_only_blue_force_contact_clears_primary_blue_force_target()
    {
        // A single friendly (blue-force) contact becomes the sim's PrimaryBlueForceTargetId.
        // When that contact's track goes stale (missed-ticks threshold reached, matching the
        // same stale-to-Lost mechanism already exercised by PdContactStaleTests), the primary
        // blue-force pointer must be recomputed — just like ApplyTargetKill/ApplyTargetBdaLost
        // already do unconditionally, and like EmitStaleLosses already does for the hostile
        // _primaryTargetId. There is no equivalent hostile contact here, isolating the
        // blue-force-only code path.
        var lifecycle = new ScenarioContactLifecycle(StaleThresholdTicks: 1);
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(11),
            [new ScenarioDetectionTrial("ucav-red", "internal-ir", "u1", "c-friendly", 1.0, RequiresActiveRadar: false)],
            contactLifecycle: lifecycle);

        sim.Tick(1, 1.0);
        Assert.Equal("u1", sim.PrimaryBlueForceTargetId);

        var t2 = sim.Tick(2, 2.0);
        Assert.Contains(t2, t => t.NewState == ContactLifecycleState.Lost);
        Assert.Equal(0, sim.ActiveCount);

        Assert.Null(sim.PrimaryBlueForceTargetId);
    }
}
