using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Comms;

/// <summary>C3-01 / DRG-390: Sim-authoritative per-unit comms-grid membership.</summary>
public sealed class CommsGridRegistryTests
{
    private static CommsGridRegistry Build() => new(new[]
    {
        new ScenarioCommsGridTransition(10, "sub-1", "OffGrid", "below comms depth"),
        new ScenarioCommsGridTransition(5, "ac-1", "OffGrid", "jammed"),
        new ScenarioCommsGridTransition(20, "sub-1", "OnGrid", "periscope depth"),
        new ScenarioCommsGridTransition(20, "ac-1", "offgrid", "duplicate — no-op"),
    });

    [Fact]
    public void Units_default_to_on_grid()
    {
        var r = Build();
        r.Advance(0);
        Assert.False(r.IsOffGrid("sub-1"));
        Assert.Null(OffGridOrderGate.Evaluate(r, "sub-1"));
        Assert.Null(OffGridOrderGate.Evaluate(null, "sub-1"));
    }

    [Fact]
    public void Transitions_apply_in_tick_then_ordinal_order_and_emit_changes()
    {
        var r = Build();
        var first = r.Advance(10);
        Assert.Equal(new[] { "ac-1", "sub-1" }, first.Select(c => c.UnitId));
        Assert.Equal(FireAbortReason.OffGrid, OffGridOrderGate.Evaluate(r, "sub-1"));
        Assert.Equal(new[] { "ac-1", "sub-1" }, r.OffGridUnitIds);
        Assert.True(r.TryGetOffGridSince("sub-1", out var since));
        Assert.Equal(10UL, since);

        var second = r.Advance(20);
        var change = Assert.Single(second); // ac-1 duplicate OffGrid is a no-op
        Assert.Equal(new CommsGridChange(20, "sub-1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "periscope depth"), change);
        Assert.False(r.IsOffGrid("sub-1"));
    }

    [Fact]
    public void Advance_is_idempotent_and_never_rewinds()
    {
        var r = Build();
        r.Advance(10);
        Assert.Empty(r.Advance(10));
        Assert.Empty(r.Advance(3));
        Assert.True(r.IsOffGrid("sub-1"));
    }

    [Fact]
    public void Position_reports_freeze_while_off_grid()
    {
        var r = Build();
        r.Advance(0);
        Assert.True(r.ReportPosition("sub-1", 9, 59.0, 20.0));
        r.Advance(10);
        Assert.False(r.ReportPosition("sub-1", 15, 60.0, 21.0));
        Assert.True(r.TryGetLastReport("sub-1", out var last));
        Assert.Equal(new UnitLastReport("sub-1", 9, 59.0, 20.0), last);
    }

    [Fact]
    public void Hash_is_deterministic_and_reflects_state()
    {
        var a = Build();
        var b = Build();
        a.Advance(10);
        b.Advance(10);
        Assert.Equal(a.ComputeHash(), b.ComputeHash());
        b.Advance(20);
        Assert.NotEqual(a.ComputeHash(), b.ComputeHash());
    }

    [Fact]
    public void Unknown_membership_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => CommsGridRegistry.ParseMembership("Sideways"));
    }

    [Fact]
    public void TryCreate_returns_null_without_transitions()
    {
        Assert.Null(CommsGridRegistry.TryCreate(null));
        Assert.Null(CommsGridRegistry.TryCreate(new ScenarioPolicyProfile(EffectivePolicy.DefaultFree)));
    }
}
