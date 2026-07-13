using System.Collections.Generic;
using System.Linq;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>Phase 2b TDD hardening (symbology): label-declutter determinism + boundary edge cases beyond
/// the T4 baseline. Replay-visual parity requires the outcome to be independent of input enumeration
/// order (req 20 rev 2 §Map and Symbology; a11y §5 shape-primary, labels are secondary).</summary>
[TestFixture]
public sealed class MapLabelDeclutterDeterminismTests
{
    private static MapLabelCandidate C(string id, MapLabelPriority p, float x, float y) => new(id, p, x, y);

    private static MapLabelDeclutterOutcome OutcomeOf(IReadOnlyList<MapLabelDeclutterResult> r, string id) =>
        r.Single(x => x.SymbolId == id).Outcome;

    [Test]
    public void Outcome_is_invariant_to_input_ordering()
    {
        // Two colliding equal-priority labels + one far away. Whichever way the caller orders the input,
        // the same symbol must win the direct slot (stable ordinal tie-break) so a replayed frame renders
        // identically regardless of enumeration order.
        var a = C("sym-a", MapLabelPriority.Hostile, 0.5f, 0.5f);
        var b = C("sym-b", MapLabelPriority.Hostile, 0.5f, 0.5f); // collides with a at radius 0.1
        var far = C("sym-c", MapLabelPriority.Hostile, 0.9f, 0.9f);

        var forward = MapLabelDeclutterProjection.Resolve(new[] { a, b, far }, 0.1f, 5, 5);
        var reversed = MapLabelDeclutterProjection.Resolve(new[] { far, b, a }, 0.1f, 5, 5);

        Assert.That(OutcomeOf(forward, "sym-a"), Is.EqualTo(OutcomeOf(reversed, "sym-a")));
        Assert.That(OutcomeOf(forward, "sym-b"), Is.EqualTo(OutcomeOf(reversed, "sym-b")));
        Assert.That(OutcomeOf(forward, "sym-a"), Is.EqualTo(MapLabelDeclutterOutcome.Shown));
        Assert.That(OutcomeOf(forward, "sym-b"), Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine));
    }

    [Test]
    public void Higher_priority_wins_the_direct_slot_over_a_colliding_lower_priority_label()
    {
        // "z-selected" sorts AFTER "a-friendly" by ordinal id, but Selected (0) outranks Friendly (3),
        // so priority — not id — must decide the single direct slot.
        var selected = C("z-selected", MapLabelPriority.Selected, 0.5f, 0.5f);
        var friendly = C("a-friendly", MapLabelPriority.Friendly, 0.5f, 0.5f); // collides

        var r = MapLabelDeclutterProjection.Resolve(new[] { friendly, selected }, 0.1f, maxDirectLabels: 1, maxLeaderLineLabels: 5);

        Assert.That(OutcomeOf(r, "z-selected"), Is.EqualTo(MapLabelDeclutterOutcome.Shown), "priority beats ordinal id");
        Assert.That(OutcomeOf(r, "a-friendly"), Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine));
    }

    [Test]
    public void Zero_max_direct_sends_the_top_priority_to_a_leader_line_then_hides_the_rest()
    {
        var a = C("a", MapLabelPriority.Selected, 0.1f, 0.1f);
        var b = C("b", MapLabelPriority.Hostile, 0.9f, 0.9f);

        var r = MapLabelDeclutterProjection.Resolve(new[] { a, b }, 0.05f, maxDirectLabels: 0, maxLeaderLineLabels: 1);

        Assert.That(OutcomeOf(r, "a"), Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine), "highest priority takes the only leader slot");
        Assert.That(OutcomeOf(r, "b"), Is.EqualTo(MapLabelDeclutterOutcome.Hidden));
    }

    [Test]
    public void Zero_collision_radius_shows_all_up_to_max_direct_even_at_identical_positions()
    {
        var a = C("a", MapLabelPriority.Hostile, 0.5f, 0.5f);
        var b = C("b", MapLabelPriority.Hostile, 0.5f, 0.5f); // same position, but radius 0 => strict < never collides

        var r = MapLabelDeclutterProjection.Resolve(new[] { a, b }, 0f, maxDirectLabels: 5, maxLeaderLineLabels: 0);

        Assert.That(OutcomeOf(r, "a"), Is.EqualTo(MapLabelDeclutterOutcome.Shown));
        Assert.That(OutcomeOf(r, "b"), Is.EqualTo(MapLabelDeclutterOutcome.Shown));
    }

    [Test]
    public void Result_preserves_input_order_regardless_of_internal_priority_sort()
    {
        var friendly = C("friendly", MapLabelPriority.Friendly, 0.1f, 0.1f);
        var selected = C("selected", MapLabelPriority.Selected, 0.9f, 0.9f);

        var r = MapLabelDeclutterProjection.Resolve(new[] { friendly, selected }, 0.05f, 5, 5);

        Assert.That(r.Select(x => x.SymbolId), Is.EqualTo(new[] { "friendly", "selected" }),
            "results echo input order even though placement is priority-sorted internally");
    }
}
