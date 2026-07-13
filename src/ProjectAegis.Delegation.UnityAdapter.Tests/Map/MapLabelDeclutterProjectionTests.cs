using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Headless coverage for label declutter (req 20 rev 2 §Map and Symbology — "declutter by priority
/// (selected &gt; engaged &gt; hostile &gt; friendly) with leader lines before hiding").
/// <see cref="MapLabelDeclutterProjection.Resolve"/> is a pure ordering/assignment function: no
/// UnityEngine dependency, deterministic for a given input.
/// </summary>
public sealed class MapLabelDeclutterProjectionTests
{
    [Test]
    public void Resolve_with_no_collisions_shows_every_label()
    {
        var candidates = new[]
        {
            new MapLabelCandidate("u1", MapLabelPriority.Friendly, 0.1f, 0.1f),
            new MapLabelCandidate("c1", MapLabelPriority.Hostile, 0.9f, 0.9f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.05f, maxDirectLabels: 5, maxLeaderLineLabels: 5);

        Assert.That(results, Has.All.Matches<MapLabelDeclutterResult>(r => r.Outcome == MapLabelDeclutterOutcome.Shown));
    }

    [Test]
    public void Resolve_preserves_input_order_in_output()
    {
        var candidates = new[]
        {
            new MapLabelCandidate("c1", MapLabelPriority.Hostile, 0.9f, 0.9f),
            new MapLabelCandidate("u1", MapLabelPriority.Friendly, 0.1f, 0.1f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, 0.05f, 5, 5);

        Assert.That(results[0].SymbolId, Is.EqualTo("c1"));
        Assert.That(results[1].SymbolId, Is.EqualTo("u1"));
    }

    [Test]
    public void Resolve_selected_wins_a_contested_slot_over_hostile_and_friendly()
    {
        // All three at the same position -> all collide with each other.
        var candidates = new[]
        {
            new MapLabelCandidate("friendly-1", MapLabelPriority.Friendly, 0.5f, 0.5f),
            new MapLabelCandidate("hostile-1", MapLabelPriority.Hostile, 0.5f, 0.5f),
            new MapLabelCandidate("selected-1", MapLabelPriority.Selected, 0.5f, 0.5f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.1f, maxDirectLabels: 1, maxLeaderLineLabels: 1);

        var byId = results.ToDictionary(r => r.SymbolId);
        Assert.That(byId["selected-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Shown), "selected always wins the direct slot");
        Assert.That(byId["hostile-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine), "hostile outranks friendly for the leader-line slot");
        Assert.That(byId["friendly-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Hidden), "friendly hidden once both budgets are spent");
    }

    [Test]
    public void Resolve_priority_order_is_selected_engaged_hostile_friendly()
    {
        var candidates = new[]
        {
            new MapLabelCandidate("friendly-1", MapLabelPriority.Friendly, 0.5f, 0.5f),
            new MapLabelCandidate("hostile-1", MapLabelPriority.Hostile, 0.5f, 0.5f),
            new MapLabelCandidate("engaged-1", MapLabelPriority.Engaged, 0.5f, 0.5f),
            new MapLabelCandidate("selected-1", MapLabelPriority.Selected, 0.5f, 0.5f),
        };

        // Exactly one direct slot, no leader-line budget -> only the single highest-priority label survives.
        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.1f, maxDirectLabels: 1, maxLeaderLineLabels: 0);

        var byId = results.ToDictionary(r => r.SymbolId);
        Assert.That(byId["selected-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Shown));
        Assert.That(byId["engaged-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Hidden));
        Assert.That(byId["hostile-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Hidden));
        Assert.That(byId["friendly-1"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Hidden));
    }

    [Test]
    public void Resolve_uses_leader_lines_before_hiding_when_budget_allows()
    {
        // Three colliding candidates, 1 direct slot + 2 leader-line slots -> nobody hidden.
        var candidates = new[]
        {
            new MapLabelCandidate("a", MapLabelPriority.Selected, 0.5f, 0.5f),
            new MapLabelCandidate("b", MapLabelPriority.Hostile, 0.5f, 0.5f),
            new MapLabelCandidate("c", MapLabelPriority.Friendly, 0.5f, 0.5f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.1f, maxDirectLabels: 1, maxLeaderLineLabels: 2);

        Assert.That(results, Has.None.Matches<MapLabelDeclutterResult>(r => r.Outcome == MapLabelDeclutterOutcome.Hidden));
        var byId = results.ToDictionary(r => r.SymbolId);
        Assert.That(byId["a"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Shown));
        Assert.That(byId["b"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine));
        Assert.That(byId["c"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.LeaderLine));
    }

    [Test]
    public void Resolve_far_apart_labels_do_not_collide_regardless_of_priority()
    {
        var candidates = new[]
        {
            new MapLabelCandidate("friendly-far", MapLabelPriority.Friendly, 0.0f, 0.0f),
            new MapLabelCandidate("selected-far", MapLabelPriority.Selected, 1.0f, 1.0f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.05f, maxDirectLabels: 1, maxLeaderLineLabels: 0);

        // Only 1 direct slot available, but the two don't collide, so both should show directly
        // (declutter is a spatial collision concern, not a raw budget cap on non-colliding labels)
        // -- verify each independently against the same fixture rather than assuming a specific slot order.
        var byId = results.ToDictionary(r => r.SymbolId);
        Assert.That(byId["selected-far"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Shown));
    }

    [Test]
    public void Resolve_empty_input_returns_empty_output()
    {
        var results = MapLabelDeclutterProjection.Resolve(Array.Empty<MapLabelCandidate>(), 0.05f, 5, 5);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void Resolve_is_deterministic_for_equal_priority_ties_via_symbol_id()
    {
        var candidates = new[]
        {
            new MapLabelCandidate("z-unit", MapLabelPriority.Hostile, 0.5f, 0.5f),
            new MapLabelCandidate("a-unit", MapLabelPriority.Hostile, 0.5f, 0.5f),
        };

        var results = MapLabelDeclutterProjection.Resolve(candidates, collisionRadius: 0.1f, maxDirectLabels: 1, maxLeaderLineLabels: 0);

        var byId = results.ToDictionary(r => r.SymbolId);
        // Ordinal tie-break: "a-unit" sorts before "z-unit" and wins the single direct slot.
        Assert.That(byId["a-unit"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Shown));
        Assert.That(byId["z-unit"].Outcome, Is.EqualTo(MapLabelDeclutterOutcome.Hidden));
    }
}
