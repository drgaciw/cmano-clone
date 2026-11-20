using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class AirOpsFsmTests
{
    [Test]
    public void Happy_path_reaches_Airborne_after_default_durations()
    {
        var ground = AirOpsUnitState.OnGround("air-1", readyForLaunch: true, hostId: "CVN-1");
        var launch = AirOpsFsm.RequestLaunch(ground);
        Assert.That(launch.Accepted, Is.True);
        Assert.That(launch.State.Phase, Is.EqualTo(AirOpsPhase.Prepping));
        Assert.That(launch.State.TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultPrepTicks));
        Assert.That(launch.State.CanAbortLaunch, Is.True);

        var state = launch.State;
        // Prep 3 ticks → Taxiing
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.Taxiing));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultTaxiTicks));

        // Taxi 2 ticks → TakingOff
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.TakingOff));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultTakeoffTicks));

        // Takeoff 1 tick → Airborne
        state = AirOpsFsm.Tick(state);
        Assert.That(state.Phase, Is.EqualTo(AirOpsPhase.Airborne));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(0));
        Assert.That(state.CanAbortLaunch, Is.False);
        Assert.That(state.ReadyForLaunch, Is.False);
    }

    [Test]
    public void Tick_multi_delta_skips_phases_deterministically()
    {
        var launch = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a"));
        // Prep3 + Taxi2 + Takeoff1 = 6 ticks to airborne
        var airborne = AirOpsFsm.Tick(launch.State, deltaTicks: 6);
        Assert.That(airborne.Phase, Is.EqualTo(AirOpsPhase.Airborne));
        Assert.That(airborne.TimeToReadyTicks, Is.EqualTo(0));
    }

    [Test]
    public void Abort_allowed_in_Prepping_Taxiing_TakingOff()
    {
        var prep = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a")).State;
        var abortPrep = AirOpsFsm.AbortLaunch(prep);
        Assert.That(abortPrep.Accepted, Is.True);
        Assert.That(abortPrep.State.Phase, Is.EqualTo(AirOpsPhase.OnGround));
        Assert.That(abortPrep.State.ReadyForLaunch, Is.True);
        Assert.That(abortPrep.State.CanAbortLaunch, Is.False);

        var taxi = AirOpsFsm.Tick(prep, deltaTicks: AirOpsFsm.DefaultPrepTicks);
        Assert.That(taxi.Phase, Is.EqualTo(AirOpsPhase.Taxiing));
        var abortTaxi = AirOpsFsm.AbortLaunch(taxi);
        Assert.That(abortTaxi.Accepted, Is.True);
        Assert.That(abortTaxi.State.Phase, Is.EqualTo(AirOpsPhase.OnGround));

        var takeoff = AirOpsFsm.Tick(taxi, deltaTicks: AirOpsFsm.DefaultTaxiTicks);
        Assert.That(takeoff.Phase, Is.EqualTo(AirOpsPhase.TakingOff));
        var abortTakeoff = AirOpsFsm.AbortLaunch(takeoff);
        Assert.That(abortTakeoff.Accepted, Is.True);
        Assert.That(abortTakeoff.State.Phase, Is.EqualTo(AirOpsPhase.OnGround));
    }

    [Test]
    public void Abort_Airborne_refuses_AIR_ABORT_TOO_LATE()
    {
        var airborne = AirOpsUnitState.Airborne("a");
        var result = AirOpsFsm.AbortLaunch(airborne);
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAbortTooLate));
        Assert.That(result.State.Phase, Is.EqualTo(AirOpsPhase.Airborne));
    }

    [Test]
    public void RequestLaunch_AIR_NOT_READY_when_not_ready_on_ground()
    {
        var notReady = AirOpsUnitState.OnGround("a", readyForLaunch: false);
        var result = AirOpsFsm.RequestLaunch(notReady);
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAirNotReady));
        Assert.That(result.State.Phase, Is.EqualTo(AirOpsPhase.OnGround));
    }

    [Test]
    public void RequestLaunch_force_bypasses_ready_gate()
    {
        var notReady = AirOpsUnitState.OnGround("a", readyForLaunch: false);
        var result = AirOpsFsm.RequestLaunch(notReady, force: true);
        Assert.That(result.Accepted, Is.True);
        Assert.That(result.State.Phase, Is.EqualTo(AirOpsPhase.Prepping));
    }

    [Test]
    public void RequestLaunch_AIR_ALREADY_AIRBORNE_and_AIR_IN_MAINTENANCE()
    {
        var air = AirOpsFsm.RequestLaunch(AirOpsUnitState.Airborne("a"));
        Assert.That(air.Accepted, Is.False);
        Assert.That(air.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAlreadyAirborne));

        var maint = AirOpsFsm.RequestLaunch(AirOpsUnitState.InMaintenance("b"));
        Assert.That(maint.Accepted, Is.False);
        Assert.That(maint.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonInMaintenance));
    }

    [Test]
    public void RequestGroupLaunch_per_unit_results_ordered_by_unit_id()
    {
        var states = new[]
        {
            AirOpsUnitState.OnGround("z", readyForLaunch: true),
            AirOpsUnitState.OnGround("a", readyForLaunch: false),
            AirOpsUnitState.Airborne("m"),
            AirOpsUnitState.InMaintenance("b"),
        };

        var results = AirOpsFsm.RequestGroupLaunch(states);
        Assert.That(results.Count, Is.EqualTo(4));
        Assert.That(results.Select(r => r.UnitId).ToArray(), Is.EqualTo(new[] { "a", "b", "m", "z" }));

        Assert.That(results[0].Result.Accepted, Is.False);
        Assert.That(results[0].Result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAirNotReady));

        Assert.That(results[1].Result.Accepted, Is.False);
        Assert.That(results[1].Result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonInMaintenance));

        Assert.That(results[2].Result.Accepted, Is.False);
        Assert.That(results[2].Result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAlreadyAirborne));

        Assert.That(results[3].Result.Accepted, Is.True);
        Assert.That(results[3].Result.State.Phase, Is.EqualTo(AirOpsPhase.Prepping));
    }

    [Test]
    public void BeginPrep_only_from_OnGround()
    {
        var airborne = AirOpsUnitState.Airborne("a");
        var result = AirOpsFsm.BeginPrep(airborne, durationTicks: 2);
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAlreadyAirborne));
    }

    [Test]
    public void StateMap_TickAll_TryLaunch_TryAbort()
    {
        var map = new AirOpsStateMap(new[]
        {
            AirOpsUnitState.OnGround("u1", readyForLaunch: true),
            AirOpsUnitState.OnGround("u2", readyForLaunch: false),
        });

        var launch1 = map.TryLaunch("u1");
        Assert.That(launch1.Accepted, Is.True);
        Assert.That(launch1.State.Phase, Is.EqualTo(AirOpsPhase.Prepping));

        var launch2 = map.TryLaunch("u2");
        Assert.That(launch2.Accepted, Is.False);
        Assert.That(launch2.RefusalReason, Is.EqualTo(AirOpsFsm.ReasonAirNotReady));

        map.TickAll(deltaTicks: 6);
        Assert.That(map.TryGet("u1", out var u1), Is.True);
        Assert.That(u1.Phase, Is.EqualTo(AirOpsPhase.Airborne));

        // re-seed mid-pipeline abort
        map.Upsert(AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("u3")).State);
        var abort = map.TryAbort("u3");
        Assert.That(abort.Accepted, Is.True);
        Assert.That(abort.State.Phase, Is.EqualTo(AirOpsPhase.OnGround));
    }

    [Test]
    public void Tick_zero_or_negative_delta_is_noop()
    {
        var prep = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a")).State;
        Assert.That(AirOpsFsm.Tick(prep, deltaTicks: 0), Is.EqualTo(prep));
        Assert.That(AirOpsFsm.Tick(prep, deltaTicks: -1), Is.EqualTo(prep));
    }

    [Test]
    public void Overridable_durations_change_timeline()
    {
        var prep = AirOpsFsm.BeginPrep(AirOpsUnitState.OnGround("a"), durationTicks: 1).State;
        // prep1 + taxi1 + takeoff1 = 3
        var air = AirOpsFsm.Tick(prep, deltaTicks: 3, taxiDurationTicks: 1, takeoffDurationTicks: 1);
        Assert.That(air.Phase, Is.EqualTo(AirOpsPhase.Airborne));
    }
}
