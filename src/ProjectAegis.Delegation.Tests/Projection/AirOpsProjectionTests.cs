using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AirOpsProjectionTests
{
    [Test]
    public void Project_ready_unit_status_is_READY_with_null_refusal()
    {
        var rows = new (string unitId, bool ready, string? platformType, string? host)[]
        {
            ("air-1", true, "Fighter", "CVN-1"),
        };

        var entries = AirOpsProjection.Project(rows);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].UnitId, Is.EqualTo("air-1"));
        Assert.That(entries[0].PlatformTypeLabel, Is.EqualTo("Fighter"));
        Assert.That(entries[0].HostLabel, Is.EqualTo("CVN-1"));
        Assert.That(entries[0].ReadyForLaunch, Is.True);
        Assert.That(entries[0].StatusLine, Is.EqualTo(AirOpsProjection.StatusReady));
        Assert.That(entries[0].StatusLine, Is.EqualTo("READY"));
        Assert.That(entries[0].RefusalCode, Is.Null);
    }

    [Test]
    public void Project_not_ready_names_AIR_NOT_READY_refusal()
    {
        var rows = new (string unitId, bool ready, string? platformType, string? host)[]
        {
            ("air-2", false, "Strike", "LHA-1"),
        };

        var entries = AirOpsProjection.Project(rows);

        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].ReadyForLaunch, Is.False);
        Assert.That(entries[0].StatusLine, Is.EqualTo("NOT READY · AIR_NOT_READY"));
        Assert.That(entries[0].StatusLine, Is.EqualTo(AirOpsProjection.StatusNotReady));
        Assert.That(entries[0].RefusalCode, Is.EqualTo(AbortReasonCatalog.Engage.AIR_NOT_READY));
        Assert.That(entries[0].RefusalCode, Is.EqualTo(AirOpsProjection.AirNotReadyCode));
    }

    [Test]
    public void Project_orders_by_unit_id_and_fills_missing_labels()
    {
        var rows = new (string unitId, bool ready, string? platformType, string? host)[]
        {
            ("u2", true, null, null),
            ("u1", false, "  ", ""),
        };

        var entries = AirOpsProjection.Project(rows);

        Assert.That(entries.Select(e => e.UnitId), Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(entries[0].PlatformTypeLabel, Is.EqualTo(AirOpsProjection.MissingLabel));
        Assert.That(entries[0].HostLabel, Is.EqualTo(AirOpsProjection.MissingLabel));
        Assert.That(entries[0].ReadyForLaunch, Is.False);
        Assert.That(entries[1].ReadyForLaunch, Is.True);
    }

    [Test]
    public void Project_func_lookups_walk_unit_ids()
    {
        var entries = AirOpsProjection.Project(
            new[] { "b", "a" },
            readyForLaunch: id => id == "a",
            platformType: id => id == "a" ? "Helo" : "UAV",
            host: _ => "BASE-1");

        Assert.That(entries.Select(e => e.UnitId), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(entries[0].ReadyForLaunch, Is.True);
        Assert.That(entries[0].PlatformTypeLabel, Is.EqualTo("Helo"));
        Assert.That(entries[1].ReadyForLaunch, Is.False);
        Assert.That(entries[1].RefusalCode, Is.EqualTo("AIR_NOT_READY"));
        Assert.That(entries[1].HostLabel, Is.EqualTo("BASE-1"));
    }

    [Test]
    public void Project_empty_or_null_returns_empty()
    {
        Assert.That(AirOpsProjection.Project(
            Array.Empty<(string, bool, string?, string?)>()), Is.Empty);
        Assert.That(AirOpsProjection.Project(
            (IReadOnlyList<(string, bool, string?, string?)>)null!), Is.Empty);
        Assert.That(AirOpsProjection.Project(
            Array.Empty<string>(), _ => true), Is.Empty);
    }

    [Test]
    public void Aggregate_counts_ready_and_formats_summary()
    {
        var entries = AirOpsProjection.Project(new (string, bool, string?, string?)[]
        {
            ("a", true, "F", "H1"),
            ("b", false, "F", "H1"),
            ("c", true, "H", "H2"),
        });

        var agg = AirOpsProjection.Aggregate(entries);

        Assert.That(agg.ReadyCount, Is.EqualTo(2));
        Assert.That(agg.TotalCount, Is.EqualTo(3));
        Assert.That(agg.SummaryLine, Is.EqualTo("READY 2/3"));
        Assert.That(agg.SummaryLine, Is.EqualTo(AirOpsProjection.FormatSummaryLine(2, 3)));
    }

    [Test]
    public void Aggregate_null_or_empty_is_Empty()
    {
        Assert.That(AirOpsProjection.Aggregate(null), Is.EqualTo(AirOpsAggregate.Empty));
        Assert.That(AirOpsProjection.Aggregate(Array.Empty<AirOpsEntry>()), Is.EqualTo(AirOpsAggregate.Empty));
        Assert.That(AirOpsAggregate.Empty.SummaryLine, Is.EqualTo("READY 0/0"));
    }

    [Test]
    public void ApplyState_formats_lines_and_header_with_aggregate()
    {
        var entries = AirOpsProjection.Project(new (string, bool, string?, string?)[]
        {
            ("air-1", true, "Fighter", "CVN-1"),
            ("air-2", false, "Strike", "CVN-1"),
        });

        var presentation = AirOpsApplyState.Apply(entries);

        Assert.That(presentation.HasReadinessData, Is.True);
        Assert.That(presentation.Rows, Has.Count.EqualTo(2));
        Assert.That(presentation.Lines, Has.Count.EqualTo(2));
        Assert.That(presentation.Lines[0], Does.Contain("air-1"));
        Assert.That(presentation.Lines[0], Does.Contain("type=Fighter"));
        Assert.That(presentation.Lines[0], Does.Contain("host=CVN-1"));
        Assert.That(presentation.Lines[0], Does.Contain("[READY]"));
        Assert.That(presentation.Lines[1], Does.Contain("[NOT READY · AIR_NOT_READY]"));
        Assert.That(presentation.Rows[0].DisplayLine, Is.EqualTo(presentation.Lines[0]));
        Assert.That(presentation.Aggregate.ReadyCount, Is.EqualTo(1));
        Assert.That(presentation.Aggregate.TotalCount, Is.EqualTo(2));
        Assert.That(presentation.HeaderLine, Is.EqualTo("AIR OPS  ·  READY 1/2"));
        Assert.That(presentation.EmptyStateLine, Is.Empty);
    }

    [Test]
    public void ApplyState_null_or_empty_returns_Empty()
    {
        Assert.That(AirOpsApplyState.Apply(null).Rows, Is.Empty);
        Assert.That(AirOpsApplyState.Apply(Array.Empty<AirOpsEntry>()), Is.SameAs(AirOpsPresentation.Empty));
        Assert.That(AirOpsPresentation.Empty.Lines, Is.Empty);
        Assert.That(AirOpsPresentation.Empty.HasReadinessData, Is.True);
    }

    [Test]
    public void ApplyState_without_readiness_data_is_honest_NO_READINESS_DATA()
    {
        var presentation = AirOpsApplyState.Apply(entries: null, hasReadinessData: false);

        Assert.That(presentation, Is.SameAs(AirOpsPresentation.NoReadinessData));
        Assert.That(presentation.HasReadinessData, Is.False);
        Assert.That(presentation.EmptyStateLine, Is.EqualTo(AirOpsApplyState.NoReadinessDataLine));
        Assert.That(presentation.EmptyStateLine, Is.EqualTo("NO READINESS DATA"));
        Assert.That(presentation.HeaderLine, Does.Contain("NO READINESS DATA"));
        Assert.That(presentation.Lines, Is.Empty);
        Assert.That(presentation.Aggregate.ReadyCount, Is.EqualTo(0));
    }

    [Test]
    public void ApplyState_with_readiness_flag_true_projects_entries()
    {
        var entries = AirOpsProjection.Project(new (string, bool, string?, string?)[]
        {
            ("x", false, null, null),
        });

        var presentation = AirOpsApplyState.Apply(entries, hasReadinessData: true);

        Assert.That(presentation.HasReadinessData, Is.True);
        Assert.That(presentation.Lines[0], Does.Contain("AIR_NOT_READY"));
    }

    [Test]
    public void ProjectLifecycle_maps_phase_timer_and_launch_abort_flags()
    {
        var states = new[]
        {
            AirOpsUnitState.OnGround("ready-1", readyForLaunch: true, hostId: "CVN-1"),
            AirOpsUnitState.OnGround("not-ready", readyForLaunch: false, hostId: "CVN-1"),
            AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("prepping")).State,
            AirOpsUnitState.Airborne("air-1"),
            AirOpsUnitState.InMaintenance("maint-1"),
        };

        var entries = AirOpsProjection.ProjectLifecycle(states);

        Assert.That(entries.Select(e => e.UnitId), Is.EqualTo(new[]
        {
            "air-1", "maint-1", "not-ready", "prepping", "ready-1",
        }));

        var ready = entries.Single(e => e.UnitId == "ready-1");
        Assert.That(ready.PhaseLabel, Is.EqualTo("OnGround"));
        Assert.That(ready.TimeToReadyTicks, Is.EqualTo(0));
        Assert.That(ready.CanLaunch, Is.True);
        Assert.That(ready.CanAbort, Is.False);
        Assert.That(ready.LaunchDisabledReason, Is.Null);
        Assert.That(ready.HostLabel, Is.EqualTo("CVN-1"));

        var notReady = entries.Single(e => e.UnitId == "not-ready");
        Assert.That(notReady.CanLaunch, Is.False);
        Assert.That(notReady.LaunchDisabledReason, Is.EqualTo(AirOpsFsm.ReasonAirNotReady));

        var prepping = entries.Single(e => e.UnitId == "prepping");
        Assert.That(prepping.PhaseLabel, Is.EqualTo("Prepping"));
        Assert.That(prepping.TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultPrepTicks));
        Assert.That(prepping.CanLaunch, Is.False);
        Assert.That(prepping.CanAbort, Is.True);
        Assert.That(prepping.StatusLine, Does.Contain("PREPPING"));

        var airborne = entries.Single(e => e.UnitId == "air-1");
        Assert.That(airborne.PhaseLabel, Is.EqualTo("Airborne"));
        Assert.That(airborne.CanLaunch, Is.False);
        Assert.That(airborne.CanAbort, Is.False);
        Assert.That(airborne.LaunchDisabledReason, Is.EqualTo(AirOpsFsm.ReasonAlreadyAirborne));

        var maint = entries.Single(e => e.UnitId == "maint-1");
        Assert.That(maint.LaunchDisabledReason, Is.EqualTo(AirOpsFsm.ReasonInMaintenance));
    }

    [Test]
    public void ProjectLifecycle_empty_or_null_returns_empty()
    {
        Assert.That(AirOpsProjection.ProjectLifecycle(null), Is.Empty);
        Assert.That(AirOpsProjection.ProjectLifecycle(Array.Empty<AirOpsUnitState>()), Is.Empty);
    }

    [Test]
    public void ApplyState_lifecycle_rows_expose_timer_and_button_flags()
    {
        var states = new[]
        {
            AirOpsFsm.RequestLaunch(AirOpsUnitState.OnGround("p1", hostId: "H")).State,
        };
        var entries = AirOpsProjection.ProjectLifecycle(states);
        var presentation = AirOpsApplyState.Apply(entries);

        Assert.That(presentation.Rows, Has.Count.EqualTo(1));
        Assert.That(presentation.Rows[0].PhaseLabel, Is.EqualTo("Prepping"));
        Assert.That(presentation.Rows[0].TimeToReadyTicks, Is.EqualTo(AirOpsFsm.DefaultPrepTicks));
        Assert.That(presentation.Rows[0].CanLaunch, Is.False);
        Assert.That(presentation.Rows[0].CanAbort, Is.True);
        Assert.That(presentation.Lines[0], Does.Contain("phase=Prepping"));
        Assert.That(presentation.Lines[0], Does.Contain("eta="));
    }
}
