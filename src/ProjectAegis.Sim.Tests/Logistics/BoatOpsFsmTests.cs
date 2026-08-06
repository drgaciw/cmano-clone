using ProjectAegis.Sim.Logistics;
using Xunit;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class BoatOpsFsmTests
{
    [Fact]
    public void Happy_path_launch_reaches_Waterborne_then_recover_to_Stowed()
    {
        var stowed = BoatOpsUnitState.Stowed(
            "rhb-1",
            hostId: "DDG-1",
            maxLaunchSeaState: 5,
            maxRecoverySeaState: 4);
        var launch = BoatOpsFsm.RequestLaunch(stowed, seaStateDouglas: 3);
        Assert.True(launch.Accepted);
        Assert.Equal(BoatOpsPhase.Prepping, launch.State.Phase);
        Assert.Equal(BoatOpsFsm.DefaultPrepTicks, launch.State.TimeToReadyTicks);
        Assert.True(launch.State.CanAbortLaunch);

        var state = launch.State;
        // Prep 2 → Launching
        state = BoatOpsFsm.Tick(state);
        state = BoatOpsFsm.Tick(state);
        Assert.Equal(BoatOpsPhase.Launching, state.Phase);
        Assert.Equal(BoatOpsFsm.DefaultLaunchTicks, state.TimeToReadyTicks);

        // Launch 2 → Waterborne
        state = BoatOpsFsm.Tick(state);
        state = BoatOpsFsm.Tick(state);
        Assert.Equal(BoatOpsPhase.Waterborne, state.Phase);
        Assert.Equal(0, state.TimeToReadyTicks);
        Assert.False(state.CanAbortLaunch);

        var recover = BoatOpsFsm.RequestRecover(state, seaStateDouglas: 3);
        Assert.True(recover.Accepted);
        Assert.Equal(BoatOpsPhase.Returning, recover.State.Phase);

        // Return 2 + Alongside 1 + Recover 2 = 5 ticks to Stowed
        state = BoatOpsFsm.Tick(recover.State, deltaTicks: 5);
        Assert.Equal(BoatOpsPhase.Stowed, state.Phase);
        Assert.Equal(0, state.TimeToReadyTicks);
    }

    [Fact]
    public void Tick_multi_delta_skips_launch_phases_deterministically()
    {
        var launch = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.Stowed("a"),
            seaStateDouglas: 0);
        // Prep2 + Launch2 = 4 ticks to waterborne
        var water = BoatOpsFsm.Tick(launch.State, deltaTicks: 4);
        Assert.Equal(BoatOpsPhase.Waterborne, water.Phase);
        Assert.Equal(0, water.TimeToReadyTicks);
    }

    [Fact]
    public void Sea_state_refuses_launch_when_above_maxLaunch_with_named_limit()
    {
        var craft = BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 3, maxRecoverySeaState: 2);
        var result = BoatOpsFsm.RequestLaunch(craft, seaStateDouglas: 4);
        Assert.False(result.Accepted);
        Assert.Equal(BoatOpsPhase.Stowed, result.State.Phase);
        Assert.Equal(BoatOpsFsm.FormatLaunchSeaStateRefusal(3), result.RefusalReason);
        Assert.Contains(BoatOpsFsm.ReasonBoatNotReady, result.RefusalReason);
        Assert.Contains("maxLaunchSeaState=3", result.RefusalReason);
    }

    [Fact]
    public void Recovery_asymmetry_allows_launch_when_sea_between_recovery_and_launch_limits()
    {
        // maxRecovery=2, maxLaunch=5 — sea=4: launch OK, recovery refused
        var craft = BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 5, maxRecoverySeaState: 2);
        var launch = BoatOpsFsm.RequestLaunch(craft, seaStateDouglas: 4);
        Assert.True(launch.Accepted);
        Assert.Equal(BoatOpsPhase.Prepping, launch.State.Phase);

        var water = BoatOpsFsm.Tick(launch.State, deltaTicks: 4);
        Assert.Equal(BoatOpsPhase.Waterborne, water.Phase);

        var recover = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 4);
        Assert.False(recover.Accepted);
        Assert.Equal(BoatOpsPhase.Waterborne, recover.State.Phase);
        Assert.Equal(BoatOpsFsm.FormatRecoverySeaStateRefusal(2), recover.RefusalReason);
        Assert.Contains(BoatOpsFsm.ReasonBoatNotReady, recover.RefusalReason!);
        Assert.Contains("maxRecoverySeaState=2", recover.RefusalReason);
    }

    [Fact]
    public void Recovery_refuses_when_sea_above_maxRecovery_naming_limit()
    {
        var water = BoatOpsUnitState.Waterborne("a", maxLaunchSeaState: 6, maxRecoverySeaState: 3);
        var result = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 5);
        Assert.False(result.Accepted);
        Assert.Contains("maxRecoverySeaState=3", result.RefusalReason);
        Assert.StartsWith(BoatOpsFsm.ReasonBoatNotReady, result.RefusalReason);
    }

    [Fact]
    public void Host_depart_while_waterborne_strands_craft()
    {
        var water = BoatOpsUnitState.Waterborne("rhb-2", hostId: "DDG-1");
        var stranded = BoatOpsFsm.HostDepartWhileWaterborne(water);
        Assert.True(stranded.Accepted);
        Assert.Equal(BoatOpsPhase.Stranded, stranded.State.Phase);
        Assert.False(stranded.State.CanAbortLaunch);

        // Already stowed — host depart is a no-op refuse
        var stowed = BoatOpsUnitState.Stowed("x");
        var refuse = BoatOpsFsm.HostDepartWhileWaterborne(stowed);
        Assert.False(refuse.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonHostDepartNotWaterborne, refuse.RefusalReason);
    }

    [Fact]
    public void Host_depart_strands_returning_and_alongside_as_well()
    {
        var water = BoatOpsUnitState.Waterborne("a");
        var returning = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 0).State;
        Assert.Equal(BoatOpsPhase.Returning, returning.Phase);
        var stranded = BoatOpsFsm.HostDepartWhileWaterborne(returning);
        Assert.True(stranded.Accepted);
        Assert.Equal(BoatOpsPhase.Stranded, stranded.State.Phase);
    }

    [Fact]
    public void Embarked_overload_refuses_launch()
    {
        var overloadPeople = BoatOpsUnitState.Stowed(
            "a",
            personnelEmbarked: 20,
            personnelCapacity: 12);
        var people = BoatOpsFsm.RequestLaunch(overloadPeople, seaStateDouglas: 0);
        Assert.False(people.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonEmbarkedOverload, people.RefusalReason);

        var overloadCargo = BoatOpsUnitState.Stowed(
            "b",
            cargoTons: 5.0,
            cargoCapacityTons: 2.0);
        var cargo = BoatOpsFsm.RequestLaunch(overloadCargo, seaStateDouglas: 0);
        Assert.False(cargo.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonEmbarkedOverload, cargo.RefusalReason);
    }

    [Fact]
    public void Abort_allowed_in_Prepping_and_Launching_only()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("a"), seaStateDouglas: 0).State;
        var abortPrep = BoatOpsFsm.AbortLaunch(prep);
        Assert.True(abortPrep.Accepted);
        Assert.Equal(BoatOpsPhase.Stowed, abortPrep.State.Phase);
        Assert.False(abortPrep.State.CanAbortLaunch);

        var launching = BoatOpsFsm.Tick(prep, deltaTicks: BoatOpsFsm.DefaultPrepTicks);
        Assert.Equal(BoatOpsPhase.Launching, launching.Phase);
        var abortLaunch = BoatOpsFsm.AbortLaunch(launching);
        Assert.True(abortLaunch.Accepted);
        Assert.Equal(BoatOpsPhase.Stowed, abortLaunch.State.Phase);

        var water = BoatOpsUnitState.Waterborne("w");
        var abortWater = BoatOpsFsm.AbortLaunch(water);
        Assert.False(abortWater.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonAbortTooLate, abortWater.RefusalReason);
    }

    [Fact]
    public void RequestLaunch_Maintenance_and_already_waterborne()
    {
        var maint = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.InMaintenance("m"),
            seaStateDouglas: 0);
        Assert.False(maint.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonInMaintenance, maint.RefusalReason);

        var water = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.Waterborne("w"),
            seaStateDouglas: 0);
        Assert.False(water.Accepted);
        Assert.Equal(BoatOpsFsm.ReasonAlreadyWaterborne, water.RefusalReason);
    }

    [Fact]
    public void ScenarioSeaState_clamps_0_to_9()
    {
        Assert.Equal(0, ScenarioSeaState.Of(-3).DouglasScale);
        Assert.Equal(9, ScenarioSeaState.Of(99).DouglasScale);
        Assert.Equal(5, ScenarioSeaState.Of(5).DouglasScale);
        Assert.Equal(0, ScenarioSeaState.Clamp(-1));
        Assert.Equal(9, ScenarioSeaState.Clamp(10));
    }

    [Fact]
    public void StateMap_TickAll_TryLaunch_TryRecover_TryAbort_TryHostDepart()
    {
        var map = new BoatOpsStateMap(
            new[]
            {
                BoatOpsUnitState.Stowed("u1", maxLaunchSeaState: 5, maxRecoverySeaState: 4),
                BoatOpsUnitState.Stowed("u2", maxLaunchSeaState: 1, maxRecoverySeaState: 0),
            },
            seaStateDouglas: 3);

        var launch1 = map.TryLaunch("u1");
        Assert.True(launch1.Accepted);
        Assert.Equal(BoatOpsPhase.Prepping, launch1.State.Phase);

        var launch2 = map.TryLaunch("u2");
        Assert.False(launch2.Accepted);
        Assert.Contains("maxLaunchSeaState=1", launch2.RefusalReason);

        map.TickAll(deltaTicks: 4);
        Assert.True(map.TryGet("u1", out var u1));
        Assert.Equal(BoatOpsPhase.Waterborne, u1.Phase);

        var recover = map.TryRecover("u1");
        Assert.True(recover.Accepted);
        Assert.Equal(BoatOpsPhase.Returning, recover.State.Phase);

        map.TickAll(deltaTicks: 5);
        Assert.True(map.TryGet("u1", out u1));
        Assert.Equal(BoatOpsPhase.Stowed, u1.Phase);

        // mid-pipeline abort
        map.Upsert(BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("u3"), 0).State);
        var abort = map.TryAbort("u3");
        Assert.True(abort.Accepted);
        Assert.Equal(BoatOpsPhase.Stowed, abort.State.Phase);

        // stranded via host depart
        map.Upsert(BoatOpsUnitState.Waterborne("u4", hostId: "H"));
        var strand = map.TryHostDepart("u4");
        Assert.True(strand.Accepted);
        Assert.Equal(BoatOpsPhase.Stranded, strand.State.Phase);
    }

    [Fact]
    public void StateMap_recovery_refused_when_sea_raised_above_recovery_limit()
    {
        var map = new BoatOpsStateMap(
            new[] { BoatOpsUnitState.Waterborne("w", maxLaunchSeaState: 6, maxRecoverySeaState: 2) },
            seaStateDouglas: 4);

        var recover = map.TryRecover("w");
        Assert.False(recover.Accepted);
        Assert.Contains("maxRecoverySeaState=2", recover.RefusalReason);
    }

    [Fact]
    public void Tick_zero_or_negative_delta_is_noop()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("a"), 0).State;
        Assert.Equal(prep, BoatOpsFsm.Tick(prep, deltaTicks: 0));
        Assert.Equal(prep, BoatOpsFsm.Tick(prep, deltaTicks: -1));
    }
}
