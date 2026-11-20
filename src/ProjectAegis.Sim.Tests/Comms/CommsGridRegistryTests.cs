using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

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

    [Test]
    public void Units_default_to_on_grid()
    {
        var r = Build();
        r.Advance(0);
        Assert.That(r.IsOffGrid("sub-1"), Is.False);
        Assert.That(OffGridOrderGate.Evaluate(r, "sub-1"), Is.Null);
        Assert.That(OffGridOrderGate.Evaluate(null, "sub-1"), Is.Null);
    }

    [Test]
    public void Transitions_apply_in_tick_then_ordinal_order_and_emit_changes()
    {
        var r = Build();
        var first = r.Advance(10);
        Assert.That(first.Select(c => c.UnitId), Is.EqualTo(new[] { "ac-1", "sub-1" }));
        Assert.That(OffGridOrderGate.Evaluate(r, "sub-1"), Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(r.OffGridUnitIds, Is.EqualTo(new[] { "ac-1", "sub-1" }));
        Assert.That(r.TryGetOffGridSince("sub-1", out var since), Is.True);
        Assert.That(since, Is.EqualTo(10UL));

        var second = r.Advance(20);
        var change = AssertEx.Single(second); // ac-1 duplicate OffGrid is a no-op
        Assert.That(
            change,
            Is.EqualTo(new CommsGridChange(20, "sub-1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "periscope depth")));
        Assert.That(r.IsOffGrid("sub-1"), Is.False);
    }

    [Test]
    public void Advance_is_idempotent_and_never_rewinds()
    {
        var r = Build();
        r.Advance(10);
        Assert.That(r.Advance(10), Is.Empty);
        Assert.That(r.Advance(3), Is.Empty);
        Assert.That(r.IsOffGrid("sub-1"), Is.True);
    }

    [Test]
    public void Position_reports_freeze_while_off_grid()
    {
        var r = Build();
        r.Advance(0);
        Assert.That(r.ReportPosition("sub-1", 9, 59.0, 20.0), Is.True);
        r.Advance(10);
        Assert.That(r.ReportPosition("sub-1", 15, 60.0, 21.0), Is.False);
        Assert.That(r.TryGetLastReport("sub-1", out var last), Is.True);
        Assert.That(last, Is.EqualTo(new UnitLastReport("sub-1", 9, 59.0, 20.0)));
    }

    [Test]
    public void Hash_is_deterministic_and_reflects_state()
    {
        var a = Build();
        var b = Build();
        a.Advance(10);
        b.Advance(10);
        Assert.That(b.ComputeHash(), Is.EqualTo(a.ComputeHash()));
        b.Advance(20);
        Assert.That(b.ComputeHash(), Is.Not.EqualTo(a.ComputeHash()));
    }

    [Test]
    public void Unknown_membership_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => CommsGridRegistry.ParseMembership("Sideways"));
    }

    [TestCase("2")]
    [TestCase("0")]
    [TestCase("1")]
    [TestCase("OnGrid,OffGrid")]
    [TestCase("OnGrid, OffGrid")]
    public void Numeric_and_combined_membership_is_rejected(string value)
    {
        Assert.Throws<InvalidDataException>(() => CommsGridRegistry.ParseMembership(value));
    }

    [Test]
    public void Off_grid_intervals_are_half_open_and_the_current_one_stays_open()
    {
        var r = new CommsGridRegistry(new[]
        {
            new ScenarioCommsGridTransition(10, "sub-1", "OffGrid"),
            new ScenarioCommsGridTransition(15, "sub-1", "OnGrid"),
            new ScenarioCommsGridTransition(30, "sub-1", "OffGrid"),
        });

        r.Advance(12);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 10), Is.True);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 12), Is.True);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 9), Is.False);

        r.Advance(20);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 14), Is.True);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 15), Is.False);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 20), Is.False);

        r.Advance(40);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 30), Is.True);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 40), Is.True);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 29), Is.False);
    }

    [Test]
    public void Closed_intervals_older_than_the_max_order_delay_are_pruned()
    {
        var r = new CommsGridRegistry(new[]
        {
            new ScenarioCommsGridTransition(10, "sub-1", "OffGrid"),
            new ScenarioCommsGridTransition(15, "sub-1", "OnGrid"),
        });
        r.Advance(15);
        var stillNeeded = 15UL + CommsGridRegistry.MaxRetainedOrderDelayTicks;
        r.Advance(stillNeeded);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 14), Is.True);

        r.Advance(stillNeeded + 1);
        Assert.That(r.WasIssuedWhileOffGrid("sub-1", 14), Is.False);
        Assert.That(r.IsOffGrid("sub-1"), Is.False);
    }

    [Test]
    public void TryCreate_returns_null_without_transitions()
    {
        Assert.That(CommsGridRegistry.TryCreate(null), Is.Null);
        Assert.That(CommsGridRegistry.TryCreate(new ScenarioPolicyProfile(EffectivePolicy.DefaultFree)), Is.Null);
    }
}
