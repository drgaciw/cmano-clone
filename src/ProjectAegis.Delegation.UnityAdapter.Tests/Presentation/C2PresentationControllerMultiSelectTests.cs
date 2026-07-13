using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 §Selection, TR-c2-005 (AC-7): shift/ctrl-click, drag-box replace/add, and N/P cycle
/// wired onto <see cref="C2PresentationController"/>'s shared <see cref="SelectionSet"/>.</summary>
[TestFixture]
public sealed class C2PresentationControllerMultiSelectTests
{
    [Test]
    public void ToggleFriendlyUnit_adds_then_removes()
    {
        var controller = new C2PresentationController();

        controller.ToggleFriendlyUnit("u1");
        Assert.That(controller.Selection.Contains("u1"), Is.True);
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u1"));

        controller.ToggleFriendlyUnit("u1");
        Assert.That(controller.Selection.Contains("u1"), Is.False);
        Assert.That(controller.SelectedUnitId, Is.Null);
    }

    [Test]
    public void ToggleFriendlyUnit_does_not_disturb_the_rest_of_an_existing_multi_select()
    {
        var controller = new C2PresentationController();
        controller.ToggleFriendlyUnit("u1");
        controller.ToggleFriendlyUnit("u2");

        controller.ToggleFriendlyUnit("u3");

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u2", "u3" }));

        controller.ToggleFriendlyUnit("u2");

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u3" }),
            "removing a mid-selection unit compacts around it (SelectionSet contract) without disturbing u1/u3");
    }

    [Test]
    public void ToggleFriendlyUnit_clears_hostile_contact_selection()
    {
        var controller = new C2PresentationController();
        controller.SelectHostileContact("c1", [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);

        controller.ToggleFriendlyUnit("u1");

        Assert.That(controller.SelectedContactId, Is.Null);
    }

    [Test]
    public void SelectFriendlyUnits_replaces_the_whole_selection_in_order()
    {
        var controller = new C2PresentationController();
        controller.ToggleFriendlyUnit("stale");

        controller.SelectFriendlyUnits(new[] { "u2", "u1" });

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u2", "u1" }));
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u2"), "anchor is the first of the replaced set");
    }

    [Test]
    public void SelectFriendlyUnits_with_null_or_empty_clears_selection()
    {
        var controller = new C2PresentationController();
        controller.ToggleFriendlyUnit("u1");

        controller.SelectFriendlyUnits(null);

        Assert.That(controller.Selection.IsEmpty, Is.True);
    }

    [Test]
    public void AddFriendlyUnits_unions_without_deselecting_existing_units()
    {
        var controller = new C2PresentationController();
        controller.ToggleFriendlyUnit("u1");

        controller.AddFriendlyUnits(new[] { "u1", "u2", "u3" });

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u2", "u3" }),
            "u1 was already selected — AddFriendlyUnits must not toggle it off");
    }

    [Test]
    public void AddFriendlyUnits_with_empty_list_is_a_no_op()
    {
        var controller = new C2PresentationController();
        controller.ToggleFriendlyUnit("u1");

        controller.AddFriendlyUnits(Array.Empty<string>());

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void CycleFriendlyUnit_replaces_selection_with_the_next_alive_unit()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");
        var oob = new[]
        {
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        };

        var moved = controller.CycleFriendlyUnit(oob, forward: true);

        Assert.That(moved, Is.True);
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u2"));
        Assert.That(controller.Selection.Count, Is.EqualTo(1), "cycle collapses a multi-select to the new anchor");
    }

    [Test]
    public void CycleFriendlyUnit_returns_false_when_no_alive_unit_exists()
    {
        var controller = new C2PresentationController();
        var oob = new[] { new OobTreeEntry("u1", false) };

        var moved = controller.CycleFriendlyUnit(oob, forward: true);

        Assert.That(moved, Is.False);
        Assert.That(controller.SelectedUnitId, Is.Null);
    }

    [Test]
    public void SelectFriendlyUnits_clears_stale_graph_highlights()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");
        controller.ApplyGraphSurfacing(ProjectAegis.Data.Catalog.InMemoryCatalogReader.BalticPatrolFixture());
        Assert.That(controller.LastGraphHighlightIds, Does.Contain("u1"));

        controller.SelectFriendlyUnits(new[] { "u2" });

        Assert.That(controller.LastGraphHighlightIds, Is.Empty);
        Assert.That(controller.LastGraphLinkChainDisplay, Is.Null);
    }
}
