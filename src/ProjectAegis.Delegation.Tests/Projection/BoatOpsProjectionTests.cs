using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class BoatOpsProjectionTests
{
    [Test]
    public void Project_stowed_craft_under_calm_sea_is_launchable()
    {
        var states = new[]
        {
            BoatOpsUnitState.Stowed("rhb-1", hostId: "DDG-1", maxLaunchSeaState: 4, maxRecoverySeaState: 3),
        };

        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 2);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].CraftId, Is.EqualTo("rhb-1"));
        Assert.That(entries[0].HostLabel, Is.EqualTo("DDG-1"));
        Assert.That(entries[0].PhaseLabel, Is.EqualTo("Stowed"));
        Assert.That(entries[0].CanLaunch, Is.True);
        Assert.That(entries[0].CanRecover, Is.False);
        Assert.That(entries[0].CanAbort, Is.False);
        Assert.That(entries[0].SeaStateLaunchOk, Is.True);
        Assert.That(entries[0].SeaStateRecoveryOk, Is.True);
        Assert.That(entries[0].StatusLine, Is.EqualTo(BoatOpsProjection.StatusStowed));
        Assert.That(entries[0].LaunchDisabledReason, Is.Null);
    }

    [Test]
    public void Project_sea_above_launch_limit_names_BOAT_NOT_READY_with_limit()
    {
        var states = new[]
        {
            BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 2, maxRecoverySeaState: 1),
        };

        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 5);

        Assert.That(entries[0].CanLaunch, Is.False);
        Assert.That(entries[0].SeaStateLaunchOk, Is.False);
        Assert.That(entries[0].LaunchDisabledReason, Is.EqualTo(BoatOpsFsm.FormatLaunchSeaStateRefusal(2)));
        Assert.That(entries[0].LaunchDisabledReason, Does.Contain(BoatOpsFsm.ReasonBoatNotReady));
        Assert.That(entries[0].LaunchDisabledReason, Does.Contain("maxLaunchSeaState=2"));
        Assert.That(entries[0].RefusalCode, Is.EqualTo(BoatOpsFsm.ReasonBoatNotReady));
        Assert.That(entries[0].StatusLine, Does.Contain("BOAT_NOT_READY"));
    }

    [Test]
    public void Project_recovery_asymmetry_waterborne_blocked_when_sea_above_recovery()
    {
        // Launched under sea=4 with maxLaunch=5, maxRecovery=2
        var states = new[]
        {
            BoatOpsUnitState.Waterborne("a", hostId: "H", maxLaunchSeaState: 5, maxRecoverySeaState: 2),
        };

        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 4);

        Assert.That(entries[0].CanLaunch, Is.False);
        Assert.That(entries[0].CanRecover, Is.False);
        Assert.That(entries[0].SeaStateLaunchOk, Is.True);
        Assert.That(entries[0].SeaStateRecoveryOk, Is.False);
        Assert.That(entries[0].RecoverDisabledReason, Is.EqualTo(BoatOpsFsm.FormatRecoverySeaStateRefusal(2)));
        Assert.That(entries[0].RecoverDisabledReason, Does.Contain("maxRecoverySeaState=2"));
        Assert.That(entries[0].StatusLine, Does.Contain("WATERBORNE"));
        Assert.That(entries[0].StatusLine, Does.Contain("recovery-blocked"));
    }

    [Test]
    public void Project_stranded_and_overload()
    {
        var water = BoatOpsUnitState.Waterborne("s1");
        var stranded = BoatOpsFsm.HostDepartWhileWaterborne(water).State;
        var overload = BoatOpsUnitState.Stowed("o1", personnelEmbarked: 30, personnelCapacity: 10);

        var entries = BoatOpsProjection.Project(new[] { stranded, overload }, seaStateDouglas: 0);

        Assert.That(entries.Select(e => e.CraftId), Is.EqualTo(new[] { "o1", "s1" }));
        var o = entries.Single(e => e.CraftId == "o1");
        Assert.That(o.CanLaunch, Is.False);
        Assert.That(o.LaunchDisabledReason, Is.EqualTo(BoatOpsFsm.ReasonEmbarkedOverload));
        Assert.That(o.StatusLine, Does.Contain("EMBARKED_OVERLOAD"));

        var s = entries.Single(e => e.CraftId == "s1");
        Assert.That(s.PhaseLabel, Is.EqualTo("Stranded"));
        Assert.That(s.StatusLine, Is.EqualTo(BoatOpsProjection.StatusStranded));
        Assert.That(s.CanRecover, Is.False);
    }

    [Test]
    public void Project_orders_by_craft_id_and_fills_missing_host()
    {
        var states = new[]
        {
            BoatOpsUnitState.Stowed("z", hostId: "H2"),
            BoatOpsUnitState.Stowed("a", hostId: null),
        };

        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 0);

        Assert.That(entries.Select(e => e.CraftId), Is.EqualTo(new[] { "a", "z" }));
        Assert.That(entries[0].HostLabel, Is.EqualTo(BoatOpsProjection.MissingLabel));
        Assert.That(entries[1].HostLabel, Is.EqualTo("H2"));
    }

    [Test]
    public void Project_empty_or_null_returns_empty()
    {
        Assert.That(BoatOpsProjection.Project(null), Is.Empty);
        Assert.That(BoatOpsProjection.Project(Array.Empty<BoatOpsUnitState>()), Is.Empty);
    }

    [Test]
    public void Project_prepping_exposes_abort_and_timer()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("p1", hostId: "H"), 0).State;
        var entries = BoatOpsProjection.Project(new[] { prep }, seaStateDouglas: 0);

        Assert.That(entries[0].PhaseLabel, Is.EqualTo("Prepping"));
        Assert.That(entries[0].TimeToReadyTicks, Is.EqualTo(BoatOpsFsm.DefaultPrepTicks));
        Assert.That(entries[0].CanLaunch, Is.False);
        Assert.That(entries[0].CanAbort, Is.True);
        Assert.That(entries[0].StatusLine, Does.Contain("PREPPING"));
    }

    [Test]
    public void Aggregate_counts_launchable_and_formats_summary()
    {
        var states = new[]
        {
            BoatOpsUnitState.Stowed("a", maxLaunchSeaState: 5),
            BoatOpsUnitState.Stowed("b", maxLaunchSeaState: 1),
            BoatOpsUnitState.Waterborne("c"),
        };
        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 2);
        var agg = BoatOpsProjection.Aggregate(entries);

        Assert.That(agg.ReadyCount, Is.EqualTo(1)); // only a
        Assert.That(agg.TotalCount, Is.EqualTo(3));
        Assert.That(agg.SummaryLine, Is.EqualTo("READY 1/3"));
    }

    [Test]
    public void Aggregate_null_or_empty_is_Empty()
    {
        Assert.That(BoatOpsProjection.Aggregate(null), Is.EqualTo(BoatOpsAggregate.Empty));
        Assert.That(BoatOpsProjection.Aggregate(Array.Empty<BoatOpsEntry>()), Is.EqualTo(BoatOpsAggregate.Empty));
        Assert.That(BoatOpsAggregate.Empty.SummaryLine, Is.EqualTo("READY 0/0"));
    }

    [Test]
    public void ApplyState_formats_lines_header_with_sea_and_load()
    {
        var states = new[]
        {
            BoatOpsUnitState.Stowed(
                "rhb-1",
                hostId: "DDG-1",
                maxLaunchSeaState: 4,
                maxRecoverySeaState: 3,
                personnelEmbarked: 6,
                personnelCapacity: 12,
                cargoTons: 0.5,
                cargoCapacityTons: 2.0),
        };
        var entries = BoatOpsProjection.Project(states, seaStateDouglas: 2);
        var presentation = BoatOpsApplyState.Apply(entries);

        Assert.That(presentation.HasBoatOpsData, Is.True);
        Assert.That(presentation.Rows, Has.Count.EqualTo(1));
        Assert.That(presentation.Lines, Has.Count.EqualTo(1));
        Assert.That(presentation.Lines[0], Does.Contain("rhb-1"));
        Assert.That(presentation.Lines[0], Does.Contain("host=DDG-1"));
        Assert.That(presentation.Lines[0], Does.Contain("phase=Stowed"));
        Assert.That(presentation.Lines[0], Does.Contain("load=6/12p"));
        Assert.That(presentation.Lines[0], Does.Contain("sea=OK"));
        Assert.That(presentation.HeaderLine, Is.EqualTo("BOAT OPS  ·  READY 1/1  ·  sea=2"));
        Assert.That(presentation.SeaStateDouglas, Is.EqualTo(2));
        Assert.That(presentation.Rows[0].CanLaunch, Is.True);
        Assert.That(presentation.Rows[0].CanRecover, Is.False);
        Assert.That(presentation.Rows[0].CanAbort, Is.False);
    }

    [Test]
    public void ApplyState_null_or_empty_returns_Empty()
    {
        Assert.That(BoatOpsApplyState.Apply(null).Rows, Is.Empty);
        Assert.That(BoatOpsApplyState.Apply(Array.Empty<BoatOpsEntry>()), Is.SameAs(BoatOpsPresentation.Empty));
        Assert.That(BoatOpsPresentation.Empty.HasBoatOpsData, Is.True);
    }

    [Test]
    public void ApplyState_without_boat_ops_data_is_honest_NO_BOAT_OPS_DATA()
    {
        var presentation = BoatOpsApplyState.Apply(entries: null, hasBoatOpsData: false);

        Assert.That(presentation, Is.SameAs(BoatOpsPresentation.NoBoatOpsData));
        Assert.That(presentation.HasBoatOpsData, Is.False);
        Assert.That(presentation.EmptyStateLine, Is.EqualTo(BoatOpsApplyState.NoBoatOpsDataLine));
        Assert.That(presentation.EmptyStateLine, Is.EqualTo("NO BOAT OPS DATA"));
        Assert.That(presentation.HeaderLine, Does.Contain("NO BOAT OPS DATA"));
        Assert.That(presentation.Lines, Is.Empty);
    }

    [Test]
    public void ApplyState_lifecycle_row_exposes_recover_and_abort_flags()
    {
        var prep = BoatOpsFsm.RequestLaunch(BoatOpsUnitState.Stowed("p1", hostId: "H"), 0).State;
        var water = BoatOpsUnitState.Waterborne("w1", hostId: "H", maxRecoverySeaState: 4);
        var entries = BoatOpsProjection.Project(new[] { prep, water }, seaStateDouglas: 1);
        var presentation = BoatOpsApplyState.Apply(entries);

        Assert.That(presentation.Rows, Has.Count.EqualTo(2));
        var p = presentation.Rows.Single(r => r.CraftId == "p1");
        Assert.That(p.PhaseLabel, Is.EqualTo("Prepping"));
        Assert.That(p.CanAbort, Is.True);
        Assert.That(p.CanLaunch, Is.False);

        var w = presentation.Rows.Single(r => r.CraftId == "w1");
        Assert.That(w.CanRecover, Is.True);
        Assert.That(w.CanLaunch, Is.False);
        Assert.That(presentation.Lines[0], Does.Contain("phase="));
    }
}
