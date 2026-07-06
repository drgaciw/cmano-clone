using System.Linq;
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

    [Fact]
    public void Lost_transition_uses_actual_previous_state_not_hardcoded_detected()
    {
        var lifecycle = new ScenarioContactLifecycle(
            StaleThresholdTicks: 1,
            ClassifyAfterTicks: 1,
            IdentifyAfterTicks: 99);
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(9),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)],
            contactLifecycle: lifecycle);

        sim.Tick(1, 1.0);
        var t2 = sim.Tick(2, 2.0);
        Assert.Contains(t2, t => t is { PreviousState: ContactLifecycleState.Classified, NewState: ContactLifecycleState.Lost });
    }

    [Fact]
    public void Stale_loss_with_multiple_simultaneous_contacts_emits_transitions_in_ordinal_contact_order()
    {
        // Two observers each hold an independent contact track. Trials are rolled in
        // ObserverId -> SensorId -> TargetId order (u1 before u2), so contact "cB"
        // (observed by u1) is inserted into the internal track dictionary before
        // contact "cA" (observed by u2) even though "cA" sorts first ordinally.
        // Both contacts stop being re-rolled once detected (RollTick skips already-
        // detected contacts) and so go stale together on the same tick.
        // ApplyTargetKill/ApplyTargetBdaLost both explicitly re-sort their lost-contact
        // lists by ContactId (StringComparer.Ordinal) before emitting transitions so
        // replay/order-log output is deterministic regardless of dictionary insertion
        // order. EmitStaleLosses (the third contact-loss path in this same class) must
        // provide the same deterministic ordering guarantee, but currently just
        // enumerates the `_tracks` dictionary directly.
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "s1", "hostile-1", "cB", 1.0),
            new ScenarioDetectionTrial("u2", "s1", "hostile-2", "cA", 1.0),
        };
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            trials,
            contactLifecycle: new ScenarioContactLifecycle(StaleThresholdTicks: 1));

        sim.Tick(1, 1.0);
        var t2 = sim.Tick(2, 2.0);

        var lostOrder = t2
            .Where(t => t.NewState == ContactLifecycleState.Lost)
            .Select(t => t.ContactId)
            .ToArray();

        Assert.Equal(new[] { "cA", "cB" }, lostOrder);
    }
}