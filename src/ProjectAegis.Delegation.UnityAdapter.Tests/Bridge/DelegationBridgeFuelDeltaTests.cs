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

    private sealed class NullOrderSink : IOrderSink
    {
        public void ApplyOrder(EntityKey entity, in Order order) { }
    }
}
