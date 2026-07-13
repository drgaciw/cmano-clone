using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2SelectionResolverTests
{
    [Test]
    public void ResolveDefaultFriendlyUnit_prefers_alive_sorted_by_id()
    {
        var id = C2SelectionResolver.ResolveDefaultFriendlyUnit(
        [
            new OobTreeEntry("u2", false),
            new OobTreeEntry("u1", true),
        ]);

        Assert.That(id, Is.EqualTo("u1"));
    }

    [Test]
    public void TryResolveFriendlyUnitFromSymbol_matches_friendly_only()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false),
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        };

        Assert.That(
            C2SelectionResolver.TryResolveFriendlyUnitFromSymbol("u1", symbols, out var unitId),
            Is.True);
        Assert.That(unitId, Is.EqualTo("u1"));
        Assert.That(
            C2SelectionResolver.TryResolveFriendlyUnitFromSymbol("c1", symbols, out _),
            Is.False);
    }

    [Test]
    public void TryResolveHostileContactFromSymbol_matches_hostile_only()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        };

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("c1", symbols, out var contactId),
            Is.True);
        Assert.That(contactId, Is.EqualTo("c1"));
    }

    // req20-rev2 Track T1 (TR-c2-005): N/P cycle next/previous friendly unit within OOB order.

    [Test]
    public void CycleUnit_forward_moves_to_next_alive_unit_in_oob_order()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
            new OobTreeEntry("u3", true),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: true), Is.EqualTo("u2"));
        Assert.That(C2SelectionResolver.CycleUnit(oob, "u2", forward: true), Is.EqualTo("u3"));
    }

    [Test]
    public void CycleUnit_forward_wraps_around_at_the_end()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u2", forward: true), Is.EqualTo("u1"));
    }

    [Test]
    public void CycleUnit_backward_wraps_around_at_the_start()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: false), Is.EqualTo("u2"));
    }

    [Test]
    public void CycleUnit_skips_destroyed_units_regardless_of_display_order()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", false), // destroyed — never a cycle target
            new OobTreeEntry("u3", true),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: true), Is.EqualTo("u3"));
    }

    [Test]
    public void CycleUnit_with_no_current_selection_returns_first_or_last_alive()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, null, forward: true), Is.EqualTo("u1"));
        Assert.That(C2SelectionResolver.CycleUnit(oob, null, forward: false), Is.EqualTo("u2"));
    }

    [Test]
    public void CycleUnit_with_current_unit_not_in_alive_list_falls_back_to_edge()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        };

        // "stale" was the last selected id but is no longer present in the OOB at all.
        Assert.That(C2SelectionResolver.CycleUnit(oob, "stale", forward: true), Is.EqualTo("u1"));
        Assert.That(C2SelectionResolver.CycleUnit(oob, "stale", forward: false), Is.EqualTo("u2"));
    }

    [Test]
    public void CycleUnit_returns_null_when_no_unit_is_alive()
    {
        var oob = new[]
        {
            new OobTreeEntry("u1", false),
            new OobTreeEntry("u2", false),
        };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: true), Is.Null);
    }

    [Test]
    public void CycleUnit_returns_null_for_an_empty_oob_instead_of_wrapping_on_nothing()
    {
        Assert.That(C2SelectionResolver.CycleUnit(Array.Empty<OobTreeEntry>(), null, forward: true), Is.Null);
        Assert.That(C2SelectionResolver.CycleUnit(Array.Empty<OobTreeEntry>(), "stale", forward: false), Is.Null);
    }

    [Test]
    public void CycleUnit_with_a_single_alive_unit_wraps_to_itself_in_both_directions()
    {
        var oob = new[] { new OobTreeEntry("u1", true) };

        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: true), Is.EqualTo("u1"));
        Assert.That(C2SelectionResolver.CycleUnit(oob, "u1", forward: false), Is.EqualTo("u1"));
    }

    [Test]
    public void CycleUnit_treats_a_null_oob_like_an_empty_one_rather_than_throwing()
    {
        // Every sibling resolver in this domain (SelectionBoxResolver, CenterOnSelectionResolver,
        // GroupOrderPlan.Build) treats a null input collection as "nothing to resolve" rather than
        // throwing. CycleUnit is called directly with host-supplied OOB data
        // (C2PresentationController.CycleFriendlyUnit does no null-check of its own), so a failed/empty
        // OOB projection must not crash N/P cycling.
        Assert.That(C2SelectionResolver.CycleUnit(null!, "u1", forward: true), Is.Null);
    }
}