namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Regression tests for ADR-020 / DRG-50: fuel drain must use actual deltaSeconds per tick,
/// not a hardcoded 1.0 per call.
/// </summary>
[TestFixture]
public sealed class DelegationBridgeFuelDeltaTests
{
    // baltic-patrol-comms: burnRateKgPerSecond=80, fuelCapacityKg=10000, jokerFuelFraction=0.25
    // JOKER fires when remaining < 2500 kg, i.e. after 7500 kg consumed = 93.75 sim-seconds.
    // At 1/60 cadence, 150 ticks = 2.5 sim-seconds; expected burn ≈ 200 kg — well below JOKER.
    // With the pre-fix hardcoded 1.0: 150 ticks × 80 kg/s = 12000 kg → JOKER fires immediately.
    [Test]
    public void At_subSecond_cadence_fuel_consumed_tracks_elapsed_simTime_not_call_count()
    {
        const double cadence = 1.0 / 60.0;
        const int ticks = 150;

        var bridge = new DelegationBridge(7, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var sink = new NullOrderSink();
        for (int i = 1; i <= ticks; i++)
        {
            bridge.Tick(new SimWorldSnapshotStub(simTime: i * cadence), sink);
        }

        // After 2.5 sim-seconds, fuel consumed ≈ 200 kg. Unit should still be NOMINAL.
        // If the bug were present, 12000 kg consumed → JOKER/BINGO would already have fired.
        Assert.That(
            bridge.Orchestrator.DecisionLog.FuelStateChanges,
            Is.Empty,
            "No band transitions should occur in only 2.5 sim-seconds of burn at 1/60 cadence");
    }

    // The existing ReplayGolden / BalticReplayHarness path uses 1.0 s/tick; verify same result.
    [Test]
    public void At_one_second_cadence_first_band_transition_fires_at_expected_simTime()
    {
        const double cadence = 1.0;
        // Drive just past 93.75 s (JOKER threshold) — 95 ticks at 1.0 s/tick.
        const int ticks = 95;

        var bridge = new DelegationBridge(7, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var sink = new NullOrderSink();
        for (int i = 1; i <= ticks; i++)
        {
            bridge.Tick(new SimWorldSnapshotStub(simTime: i * cadence), sink);
        }

        Assert.That(
            bridge.Orchestrator.DecisionLog.FuelStateChanges,
            Has.Count.GreaterThanOrEqualTo(1),
            "JOKER transition must fire within 95 sim-seconds at 1.0 s/tick cadence");

        var first = bridge.Orchestrator.DecisionLog.FuelStateChanges[0];
        Assert.That(first.SimTime, Is.GreaterThan(90.0).And.LessThan(100.0),
            "JOKER fires at ~93.75 sim-seconds");
        Assert.That(first.NewState, Is.EqualTo("JOKER"));
    }

    // Positive counterpart to the sub-second test above: a fix that simply stopped
    // draining fuel would satisfy an "Is.Empty" assertion, so pin the sim-time at
    // which the band actually flips. Analytic threshold is 7500 kg / 80 kg/s = 93.75 s,
    // and at 1/60 cadence the first qualifying sample lands within one tick of it.
    [Test]
    public void Band_transition_fires_at_the_same_simTime_under_subSecond_cadence()
    {
        const double cadence = 1.0 / 60.0;
        const int ticks = 5700; // 95 sim-seconds

        var bridge = new DelegationBridge(7, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();

        var sink = new NullOrderSink();
        for (int i = 1; i <= ticks; i++)
        {
            bridge.Tick(new SimWorldSnapshotStub(simTime: i * cadence), sink);
        }

        var changes = bridge.Orchestrator.DecisionLog.FuelStateChanges;
        Assert.That(changes, Has.Count.GreaterThanOrEqualTo(1), "JOKER must fire within 95 sim-seconds");
        Assert.That(changes[0].NewState, Is.EqualTo("JOKER"));
        Assert.That(
            changes[0].SimTime,
            Is.EqualTo(93.75).Within(2.0 * cadence),
            "Band transition sim-time must be cadence-independent, not call-count driven");
    }

    // Guards the epoch fallback: units that register after the sim has been running
    // must be charged only from the moment they exist, never for the whole [0, N] gap.
    [Test]
    public void Unit_registered_after_sim_start_is_not_retro_charged_from_epoch()
    {
        const double cadence = 1.0;

        var bridge = new DelegationBridge(7, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        bridge.BeginExecution();

        var sink = new NullOrderSink();

        // 200 sim-seconds with an empty registry. Retro-charging this gap would burn
        // 16 000 kg against a 10 000 kg tank and flip straight to BINGO.
        for (int i = 1; i <= 200; i++)
        {
            bridge.Tick(new SimWorldSnapshotStub(simTime: i * cadence), sink);
        }

        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");

        for (int i = 201; i <= 210; i++)
        {
            bridge.Tick(new SimWorldSnapshotStub(simTime: i * cadence), sink);
        }

        Assert.That(
            bridge.Orchestrator.DecisionLog.FuelStateChanges,
            Is.Empty,
            "Only 10 sim-seconds of burn have elapsed for this unit; no band transition is due");
    }

    private sealed class NullOrderSink : IOrderSink
    {
        public void ApplyOrder(EntityKey entity, in Order order) { }
    }
}
