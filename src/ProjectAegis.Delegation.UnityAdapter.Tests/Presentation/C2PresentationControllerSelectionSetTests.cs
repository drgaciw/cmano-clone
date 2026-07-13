using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Phase 0 (TR-c2-005): the controller's new <see cref="SelectionSet"/> stays consistent with
/// the pre-rev-2 single-select contract — <c>SelectedUnitId</c> mirrors the set anchor.</summary>
[TestFixture]
public sealed class C2PresentationControllerSelectionSetTests
{
    [Test]
    public void SelectFriendlyUnit_yields_a_set_of_one_with_matching_anchor()
    {
        var controller = new C2PresentationController();

        controller.SelectFriendlyUnit("u1");

        Assert.That(controller.SelectedUnitId, Is.EqualTo("u1"));
        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1" }));
        Assert.That(controller.Selection.PrimaryUnitId, Is.EqualTo(controller.SelectedUnitId));
    }

    [Test]
    public void Reselecting_a_friendly_unit_replaces_rather_than_accumulates()
    {
        var controller = new C2PresentationController();

        controller.SelectFriendlyUnit("u1");
        controller.SelectFriendlyUnit("u2");

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u2" }));
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u2"));
    }

    [Test]
    public void SelectHostileContact_clears_the_selection_set()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");

        controller.SelectHostileContact(
            "c1",
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);

        Assert.That(controller.Selection.IsEmpty, Is.True);
        Assert.That(controller.SelectedUnitId, Is.Null);
    }

    [Test]
    public void Default_selection_populates_the_set_anchor()
    {
        var controller = new C2PresentationController();

        controller.ApplyDefaultSelection([new OobTreeEntry("u1", true)]);

        Assert.That(controller.Selection.PrimaryUnitId, Is.EqualTo("u1"));
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u1"));
    }
}
