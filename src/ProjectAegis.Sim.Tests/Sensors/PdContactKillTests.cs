using System.Linq;
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

    [Fact]
    public void Kill_with_multiple_contacts_on_same_target_emits_transitions_in_ordinal_contact_order()
    {
        // Two observers track the same target under different contact ids. Trials are
        // rolled in ObserverId -> SensorId -> TargetId order (u1 before u2), so contact
        // "cB" (observed by u1) is inserted into the internal track dictionary before
        // contact "cA" (observed by u2) even though "cA" sorts first ordinally.
        // ApplyTargetBdaLost explicitly re-sorts its lost-contact list by ContactId
        // (StringComparer.Ordinal) before emitting transitions so replay/order-log
        // output is deterministic regardless of dictionary insertion order.
        // ApplyTargetKill must provide the same deterministic ordering guarantee.
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "s1", "hostile-1", "cB", 1.0),
            new ScenarioDetectionTrial("u2", "s1", "hostile-1", "cA", 1.0),
        };
        var sim = new PdDetectionContactSimulator(SimSeed.FromScenario(42), trials);

        sim.Tick(1, 1.0);
        var transitions = sim.ApplyTargetKill(2, 2.0, "hostile-1");

        Assert.Equal(
            new[] { "cA", "cB" },
            transitions.Select(t => t.ContactId).ToArray());
    }
}