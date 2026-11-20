using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class BoatOpsFsmTests
{
    [Test]
    public void Happy_path_launch_reaches_Waterborne_then_recover_to_Stowed()
    {
        var stowed = BoatOpsUnitState.Stowed(
            "rhb-1",
            hostId: "DDG-1",
            maxLaunchSeaState: 5,
            maxRecoverySeaState: 4);
        var launch = BoatOpsFsm.RequestLaunch(stowed, seaStateDouglas: 3);
        Assert.That(launch.Accepted, Is.True);
        Assert.That(launch.State.Phase, Is.EqualTo(BoatOpsPhase.Prepping));
        Assert.That(launch.State.TimeToReadyTicks, Is.EqualTo(BoatOpsFsm.DefaultPrepTicks));
        Assert.That(launch.State.CanAbortLaunch, Is.True);

        var state = launch.State;
        // Prep 2 → Launching
        state = BoatOpsFsm.Tick(state);
        state = BoatOpsFsm.Tick(state);
        Assert.That(state.Phase, Is.EqualTo(BoatOpsPhase.Launching));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(BoatOpsFsm.DefaultLaunchTicks));

        // Launch 2 → Waterborne
        state = BoatOpsFsm.Tick(state);
        state = BoatOpsFsm.Tick(state);
        Assert.That(state.Phase, Is.EqualTo(BoatOpsPhase.Waterborne));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(0));
        Assert.That(state.CanAbortLaunch, Is.False);

        var recover = BoatOpsFsm.RequestRecover(state, seaStateDouglas: 3);
        Assert.That(recover.Accepted, Is.True);
        Assert.That(recover.State.Phase, Is.EqualTo(BoatOpsPhase.Returning));

        // Return 2 + Alongside 1 + Recover 2 = 5 ticks to Stowed
        state = BoatOpsFsm.Tick(recover.State, deltaTicks: 5);
        Assert.That(state.Phase, Is.EqualTo(BoatOpsPhase.Stowed));
        Assert.That(state.TimeToReadyTicks, Is.EqualTo(0));
    }

    [Test]
    public void Tick_multi_delta_skips_launch_phases_deterministically()
    {
        var launch = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.Stowed("a"),
            seaStateDouglas: 0);
        // Prep2 + Launch2 = 4 ticks to waterborne
        var water = BoatOpsFsm.Tick(launch.State, deltaTicks: 4);
        Assert.That(water.Phase, Is.EqualTo(BoatOpsPhase.Waterborne));
        Assert.That(water.TimeToReadyTicks, Is.EqualTo(0));
    }

    [Test]
    public void Sea_state_refuses_launch_when_above_maxLaunch_with_named_limit()
    {
        var craft = BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 3, maxRecoverySeaState: 2);
        var result = BoatOpsFsm.RequestLaunch(craft, seaStateDouglas: 4);
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.State.Phase, Is.EqualTo(BoatOpsPhase.Stowed));
        Assert.That(result.RefusalReason, Is.EqualTo(BoatOpsFsm.FormatLaunchSeaStateRefusal(3)));
        Assert.That(result.RefusalReason, Does.Contain(BoatOpsFsm.ReasonBoatNotReady));
        Assert.That(result.RefusalReason, Does.Contain("maxLaunchSeaState=3"));
    }

    [Test]
    public void Recovery_asymmetry_allows_launch_when_sea_between_recovery_and_launch_limits()
    {
        // maxRecovery=2, maxLaunch=5 — sea=4: launch OK, recovery refused
        var craft = BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 5, maxRecoverySeaState: 2);
        var launch = BoatOpsFsm.RequestLaunch(craft, seaStateDouglas: 4);
        Assert.That(launch.Accepted, Is.True);
        Assert.That(launch.State.Phase, Is.EqualTo(BoatOpsPhase.Prepping));

        var water = BoatOpsFsm.Tick(launch.State, deltaTicks: 4);
        Assert.That(water.Phase, Is.EqualTo(BoatOpsPhase.Waterborne));

        var recover = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 4);
        Assert.That(recover.Accepted, Is.False);
        Assert.That(recover.State.Phase, Is.EqualTo(BoatOpsPhase.Waterborne));
        Assert.That(recover.RefusalReason, Is.EqualTo(BoatOpsFsm.FormatRecoverySeaStateRefusal(2)));
        Assert.That(recover.RefusalReason!, Does.Contain(BoatOpsFsm.ReasonBoatNotReady));
        Assert.That(recover.RefusalReason, Does.Contain("maxRecoverySeaState=2"));
    }

    [Test]
    public void Recovery_refuses_when_sea_above_maxRecovery_naming_limit()
    {
        var water = BoatOpsUnitState.Waterborne("a", maxLaunchSeaState: 6, maxRecoverySeaState: 3);
        var result = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 5);
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RefusalReason, Does.Contain("maxRecoverySeaState=3"));
        Assert.That(result.RefusalReason, Does.StartWith(BoatOpsFsm.ReasonBoatNotReady));
    }

    [Test]
    public void Host_depart_while_waterborne_strands_craft()
    {
        var water = BoatOpsUnitState.Waterborne("rhb-2", hostId: "DDG-1");
        var stranded = BoatOpsFsm.HostDepartWhileWaterborne(water);
        Assert.That(stranded.Accepted, Is.True);
        Assert.That(stranded.State.Phase, Is.EqualTo(BoatOpsPhase.Stranded));
        Assert.That(stranded.State.CanAbortLaunch, Is.False);

        // Already stowed — host depart is a no-op refuse
        var stowed = BoatOpsUnitState.Stowed("x");
        var refuse = BoatOpsFsm.HostDepartWhileWaterborne(stowed);
        Assert.That(refuse.Accepted, Is.False);
        Assert.That(refuse.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonHostDepartNotWaterborne));
    }

    [Test]
    public void Host_depart_strands_returning_and_alongside_as_well()
    {
        var water = BoatOpsUnitState.Waterborne("a");
        var returning = BoatOpsFsm.RequestRecover(water, seaStateDouglas: 0).State;
        Assert.That(returning.Phase, Is.EqualTo(BoatOpsPhase.Returning));
        var stranded = BoatOpsFsm.HostDepartWhileWaterborne(returning);
        Assert.That(stranded.Accepted, Is.True);
        Assert.That(stranded.State.Phase, Is.EqualTo(BoatOpsPhase.Stranded));
    }

    [Test]
    public void Embarked_overload_refuses_launch()
    {
        var overloadPeople = BoatOpsUnitState.Stowed(
            "a",
            personnelEmbarked: 20,
            personnelCapacity: 12);
        var people = BoatOpsFsm.RequestLaunch(overloadPeople, seaStateDouglas: 0);
        Assert.That(people.Accepted, Is.False);
        Assert.That(people.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonEmbarkedOverload));

        var overloadCargo = BoatOpsUnitState.Stowed(
            "b",
            cargoTons: 5.0,
            cargoCapacityTons: 2.0);
        var cargo = BoatOpsFsm.RequestLaunch(overloadCargo, seaStateDouglas: 0);
        Assert.That(cargo.Accepted, Is.False);
        Assert.That(cargo.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonEmbarkedOverload));
    }

    [Test]
    public void Abort_allowed_in_Prepping_and_Launching_only()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("a"), seaStateDouglas: 0).State;
        var abortPrep = BoatOpsFsm.AbortLaunch(prep);
        Assert.That(abortPrep.Accepted, Is.True);
        Assert.That(abortPrep.State.Phase, Is.EqualTo(BoatOpsPhase.Stowed));
        Assert.That(abortPrep.State.CanAbortLaunch, Is.False);

        var launching = BoatOpsFsm.Tick(prep, deltaTicks: BoatOpsFsm.DefaultPrepTicks);
        Assert.That(launching.Phase, Is.EqualTo(BoatOpsPhase.Launching));
        var abortLaunch = BoatOpsFsm.AbortLaunch(launching);
        Assert.That(abortLaunch.Accepted, Is.True);
        Assert.That(abortLaunch.State.Phase, Is.EqualTo(BoatOpsPhase.Stowed));

        var water = BoatOpsUnitState.Waterborne("w");
        var abortWater = BoatOpsFsm.AbortLaunch(water);
        Assert.That(abortWater.Accepted, Is.False);
        Assert.That(abortWater.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonAbortTooLate));
    }

    [Test]
    public void RequestLaunch_Maintenance_and_already_waterborne()
    {
        var maint = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.InMaintenance("m"),
            seaStateDouglas: 0);
        Assert.That(maint.Accepted, Is.False);
        Assert.That(maint.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonInMaintenance));

        var water = BoatOpsFsm.RequestLaunch(
            BoatOpsUnitState.Waterborne("w"),
            seaStateDouglas: 0);
        Assert.That(water.Accepted, Is.False);
        Assert.That(water.RefusalReason, Is.EqualTo(BoatOpsFsm.ReasonAlreadyWaterborne));
    }

    [Test]
    public void ScenarioSeaState_clamps_0_to_9()
    {
        Assert.That(ScenarioSeaState.Of(-3).DouglasScale, Is.EqualTo(0));
        Assert.That(ScenarioSeaState.Of(99).DouglasScale, Is.EqualTo(9));
        Assert.That(ScenarioSeaState.Of(5).DouglasScale, Is.EqualTo(5));
        Assert.That(ScenarioSeaState.Clamp(-1), Is.EqualTo(0));
        Assert.That(ScenarioSeaState.Clamp(10), Is.EqualTo(9));
    }

    [Test]
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
        Assert.That(launch1.Accepted, Is.True);
        Assert.That(launch1.State.Phase, Is.EqualTo(BoatOpsPhase.Prepping));

        var launch2 = map.TryLaunch("u2");
        Assert.That(launch2.Accepted, Is.False);
        Assert.That(launch2.RefusalReason, Does.Contain("maxLaunchSeaState=1"));

        map.TickAll(deltaTicks: 4);
        Assert.That(map.TryGet("u1", out var u1), Is.True);
        Assert.That(u1.Phase, Is.EqualTo(BoatOpsPhase.Waterborne));

        var recover = map.TryRecover("u1");
        Assert.That(recover.Accepted, Is.True);
        Assert.That(recover.State.Phase, Is.EqualTo(BoatOpsPhase.Returning));

        map.TickAll(deltaTicks: 5);
        Assert.That(map.TryGet("u1", out u1), Is.True);
        Assert.That(u1.Phase, Is.EqualTo(BoatOpsPhase.Stowed));

        // mid-pipeline abort
        map.Upsert(BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("u3"), 0).State);
        var abort = map.TryAbort("u3");
        Assert.That(abort.Accepted, Is.True);
        Assert.That(abort.State.Phase, Is.EqualTo(BoatOpsPhase.Stowed));

        // stranded via host depart
        map.Upsert(BoatOpsUnitState.Waterborne("u4", hostId: "H"));
        var strand = map.TryHostDepart("u4");
        Assert.That(strand.Accepted, Is.True);
        Assert.That(strand.State.Phase, Is.EqualTo(BoatOpsPhase.Stranded));
    }

    [Test]
    public void StateMap_recovery_refused_when_sea_raised_above_recovery_limit()
    {
        var map = new BoatOpsStateMap(
            new[] { BoatOpsUnitState.Waterborne("w", maxLaunchSeaState: 6, maxRecoverySeaState: 2) },
            seaStateDouglas: 4);

        var recover = map.TryRecover("w");
        Assert.That(recover.Accepted, Is.False);
        Assert.That(recover.RefusalReason, Does.Contain("maxRecoverySeaState=2"));
    }

    [Test]
    public void Tick_zero_or_negative_delta_is_noop()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("a"), 0).State;
        Assert.That(BoatOpsFsm.Tick(prep, deltaTicks: 0), Is.EqualTo(prep));
        Assert.That(BoatOpsFsm.Tick(prep, deltaTicks: -1), Is.EqualTo(prep));
    }
}
