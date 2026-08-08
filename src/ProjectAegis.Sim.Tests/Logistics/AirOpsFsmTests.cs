using ProjectAegis.Sim.Logistics;
using Xunit;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class AirOpsFsmTests
{
    [Fact]
    public void Happy_path_reaches_Airborne_after_default_durations()
    {
        var ground = AirOpsUnitState.OnGround("air-1", readyForLaunch: true, hostId: "CVN-1");
        var launch = AirOpsFsm.RequestLaunch(ground);
        Assert.True(launch.Accepted);
        Assert.Equal(AirOpsPhase.Prepping, launch.State.Phase);
        Assert.Equal(AirOpsFsm.DefaultPrepTicks, launch.State.TimeToReadyTicks);
        Assert.True(launch.State.CanAbortLaunch);

        var state = launch.State;
        // Prep 3 ticks → Taxiing
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        Assert.Equal(AirOpsPhase.Taxiing, state.Phase);
        Assert.Equal(AirOpsFsm.DefaultTaxiTicks, state.TimeToReadyTicks);

        // Taxi 2 ticks → TakingOff
        state = AirOpsFsm.Tick(state);
        state = AirOpsFsm.Tick(state);
        Assert.Equal(AirOpsPhase.TakingOff, state.Phase);
        Assert.Equal(AirOpsFsm.DefaultTakeoffTicks, state.TimeToReadyTicks);

        // Takeoff 1 tick → Airborne
        state = AirOpsFsm.Tick(state);
        Assert.Equal(AirOpsPhase.Airborne, state.Phase);
        Assert.Equal(0, state.TimeToReadyTicks);
        Assert.False(state.CanAbortLaunch);
        Assert.False(state.ReadyForLaunch);
    }

    [Fact]
    public void Tick_multi_delta_skips_phases_deterministically()
    {
        var launch = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a"));
        // Prep3 + Taxi2 + Takeoff1 = 6 ticks to airborne
        var airborne = AirOpsFsm.Tick(launch.State, deltaTicks: 6);
        Assert.Equal(AirOpsPhase.Airborne, airborne.Phase);
        Assert.Equal(0, airborne.TimeToReadyTicks);
    }

    [Fact]
    public void Abort_allowed_in_Prepping_Taxiing_TakingOff()
    {
        var prep = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a")).State;
        var abortPrep = AirOpsFsm.AbortLaunch(prep);
        Assert.True(abortPrep.Accepted);
        Assert.Equal(AirOpsPhase.OnGround, abortPrep.State.Phase);
        Assert.True(abortPrep.State.ReadyForLaunch);
        Assert.False(abortPrep.State.CanAbortLaunch);

        var taxi = AirOpsFsm.Tick(prep, deltaTicks: AirOpsFsm.DefaultPrepTicks);
        Assert.Equal(AirOpsPhase.Taxiing, taxi.Phase);
        var abortTaxi = AirOpsFsm.AbortLaunch(taxi);
        Assert.True(abortTaxi.Accepted);
        Assert.Equal(AirOpsPhase.OnGround, abortTaxi.State.Phase);

        var takeoff = AirOpsFsm.Tick(taxi, deltaTicks: AirOpsFsm.DefaultTaxiTicks);
        Assert.Equal(AirOpsPhase.TakingOff, takeoff.Phase);
        var abortTakeoff = AirOpsFsm.AbortLaunch(takeoff);
        Assert.True(abortTakeoff.Accepted);
        Assert.Equal(AirOpsPhase.OnGround, abortTakeoff.State.Phase);
    }

    [Fact]
    public void Abort_Airborne_refuses_AIR_ABORT_TOO_LATE()
    {
        var airborne = AirOpsUnitState.Airborne("a");
        var result = AirOpsFsm.AbortLaunch(airborne);
        Assert.False(result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAbortTooLate, result.RefusalReason);
        Assert.Equal(AirOpsPhase.Airborne, result.State.Phase);
    }

    [Fact]
    public void RequestLaunch_AIR_NOT_READY_when_not_ready_on_ground()
    {
        var notReady = AirOpsUnitState.OnGround("a", readyForLaunch: false);
        var result = AirOpsFsm.RequestLaunch(notReady);
        Assert.False(result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAirNotReady, result.RefusalReason);
        Assert.Equal(AirOpsPhase.OnGround, result.State.Phase);
    }

    [Fact]
    public void RequestLaunch_force_bypasses_ready_gate()
    {
        var notReady = AirOpsUnitState.OnGround("a", readyForLaunch: false);
        var result = AirOpsFsm.RequestLaunch(notReady, force: true);
        Assert.True(result.Accepted);
        Assert.Equal(AirOpsPhase.Prepping, result.State.Phase);
    }

    [Fact]
    public void RequestLaunch_AIR_ALREADY_AIRBORNE_and_AIR_IN_MAINTENANCE()
    {
        var air = AirOpsFsm.RequestLaunch(AirOpsUnitState.Airborne("a"));
        Assert.False(air.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAlreadyAirborne, air.RefusalReason);

        var maint = AirOpsFsm.RequestLaunch(AirOpsUnitState.InMaintenance("b"));
        Assert.False(maint.Accepted);
        Assert.Equal(AirOpsFsm.ReasonInMaintenance, maint.RefusalReason);
    }

    [Fact]
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
        Assert.Equal(4, results.Count);
        Assert.Equal(new[] { "a", "b", "m", "z" }, results.Select(r => r.UnitId).ToArray());

        Assert.False(results[0].Result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAirNotReady, results[0].Result.RefusalReason);

        Assert.False(results[1].Result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonInMaintenance, results[1].Result.RefusalReason);

        Assert.False(results[2].Result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAlreadyAirborne, results[2].Result.RefusalReason);

        Assert.True(results[3].Result.Accepted);
        Assert.Equal(AirOpsPhase.Prepping, results[3].Result.State.Phase);
    }

    [Fact]
    public void BeginPrep_only_from_OnGround()
    {
        var airborne = AirOpsUnitState.Airborne("a");
        var result = AirOpsFsm.BeginPrep(airborne, durationTicks: 2);
        Assert.False(result.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAlreadyAirborne, result.RefusalReason);
    }

    [Fact]
    public void StateMap_TickAll_TryLaunch_TryAbort()
    {
        var map = new AirOpsStateMap(new[]
        {
            AirOpsUnitState.OnGround("u1", readyForLaunch: true),
            AirOpsUnitState.OnGround("u2", readyForLaunch: false),
        });

        var launch1 = map.TryLaunch("u1");
        Assert.True(launch1.Accepted);
        Assert.Equal(AirOpsPhase.Prepping, launch1.State.Phase);

        var launch2 = map.TryLaunch("u2");
        Assert.False(launch2.Accepted);
        Assert.Equal(AirOpsFsm.ReasonAirNotReady, launch2.RefusalReason);

        map.TickAll(deltaTicks: 6);
        Assert.True(map.TryGet("u1", out var u1));
        Assert.Equal(AirOpsPhase.Airborne, u1.Phase);

        // re-seed mid-pipeline abort
        map.Upsert(AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("u3")).State);
        var abort = map.TryAbort("u3");
        Assert.True(abort.Accepted);
        Assert.Equal(AirOpsPhase.OnGround, abort.State.Phase);
    }

    [Fact]
    public void Tick_zero_or_negative_delta_is_noop()
    {
        var prep = AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("a")).State;
        Assert.Equal(prep, AirOpsFsm.Tick(prep, deltaTicks: 0));
        Assert.Equal(prep, AirOpsFsm.Tick(prep, deltaTicks: -1));
    }

    [Fact]
    public void Overridable_durations_change_timeline()
    {
        var prep = AirOpsFsm.BeginPrep(AirOpsUnitState.OnGround("a"), durationTicks: 1).State;
        // prep1 + taxi1 + takeoff1 = 3
        var air = AirOpsFsm.Tick(prep, deltaTicks: 3, taxiDurationTicks: 1, takeoffDurationTicks: 1);
        Assert.Equal(AirOpsPhase.Airborne, air.Phase);
    }
}
