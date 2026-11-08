using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Phase 0 contract (req 20 §Selection, TR-c2-005): ordered, de-duplicated selection set;
/// single-select is a set of one.</summary>
[TestFixture]
public sealed class SelectionSetTests
{
    [Test]
    public void New_set_is_empty()
    {
        var set = new SelectionSet();

        Assert.That(set.IsEmpty, Is.True);
        Assert.That(set.Count, Is.EqualTo(0));
        Assert.That(set.PrimaryUnitId, Is.Null);
        Assert.That(set.OrderedTargetIds, Is.Empty);
    }

    [Test]
    public void ReplaceWith_makes_a_set_of_one_and_sets_primary()
    {
        var set = new SelectionSet();

        set.ReplaceWith("u1");

        Assert.That(set.Count, Is.EqualTo(1));
        Assert.That(set.PrimaryUnitId, Is.EqualTo("u1"));
        Assert.That(set.OrderedTargetIds, Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void ReplaceWith_discards_prior_selection()
    {
        var set = new SelectionSet();
        set.Add("u1");
        set.Add("u2");

        set.ReplaceWith("u3");

        Assert.That(set.OrderedTargetIds, Is.EqualTo(new[] { "u3" }));
    }

    [Test]
    public void ReplaceWith_null_or_empty_clears()
    {
        var set = new SelectionSet();
        set.Add("u1");

        set.ReplaceWith(null);

        Assert.That(set.IsEmpty, Is.True);
    }

    [Test]
    public void Add_preserves_insertion_order_and_anchor()
    {
        var set = new SelectionSet();

        set.Add("u2");
        set.Add("u1");
        set.Add("u3");

        Assert.That(set.OrderedTargetIds, Is.EqualTo(new[] { "u2", "u1", "u3" }));
        Assert.That(set.PrimaryUnitId, Is.EqualTo("u2"), "anchor is the first-added unit");
    }

    [Test]
    public void Add_is_idempotent_for_duplicates()
    {
        var set = new SelectionSet();

        Assert.That(set.Add("u1"), Is.True);
        Assert.That(set.Add("u1"), Is.False);
        Assert.That(set.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_ignores_null_or_empty()
    {
        var set = new SelectionSet();

        Assert.That(set.Add(null), Is.False);
        Assert.That(set.Add(string.Empty), Is.False);
        Assert.That(set.IsEmpty, Is.True);
    }

    [Test]
    public void Toggle_adds_then_removes()
    {
        var set = new SelectionSet();

        Assert.That(set.Toggle("u1"), Is.True, "first toggle adds");
        Assert.That(set.Contains("u1"), Is.True);
        Assert.That(set.Toggle("u1"), Is.False, "second toggle removes");
        Assert.That(set.Contains("u1"), Is.False);
    }

    [Test]
    public void Remove_returns_false_when_absent_and_compacts_order()
    {
        var set = new SelectionSet();
        set.Add("u1");
        set.Add("u2");
        set.Add("u3");

        Assert.That(set.Remove("u9"), Is.False);
        Assert.That(set.Remove("u2"), Is.True);
        Assert.That(set.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u3" }));
    }

    [Test]
    public void Removing_the_anchor_promotes_the_next_unit()
    {
        var set = new SelectionSet();
        set.Add("u1");
        set.Add("u2");

        set.Remove("u1");

        Assert.That(set.PrimaryUnitId, Is.EqualTo("u2"));
    }
}
