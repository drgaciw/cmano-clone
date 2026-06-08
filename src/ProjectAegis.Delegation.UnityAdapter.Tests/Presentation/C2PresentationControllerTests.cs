using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

public sealed class C2PresentationControllerTests
{
    [Test]
    public void ApplyDefaultSelection_skips_when_unit_already_selected()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u2");

        controller.ApplyDefaultSelection(
        [
            new OobTreeEntry("u1", true),
            new OobTreeEntry("u2", true),
        ]);

        Assert.That(controller.SelectedUnitId, Is.EqualTo("u2"));
    }

    [Test]
    public void ApplyDefaultSelection_skips_when_contact_already_selected()
    {
        var controller = new C2PresentationController();
        var contacts = new[]
        {
            new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0),
        };
        controller.SelectHostileContact("c1", contacts);

        controller.ApplyDefaultSelection([new OobTreeEntry("u1", true)]);

        Assert.That(controller.SelectedContactId, Is.EqualTo("c1"));
        Assert.That(controller.SelectedUnitId, Is.Null);
    }

    [Test]
    public void SelectHostileContact_clears_friendly_selection()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");
        controller.SelectHostileContact(
            "c1",
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);

        Assert.That(controller.SelectedUnitId, Is.Null);
        Assert.That(controller.SelectedContactId, Is.EqualTo("c1"));
        Assert.That(controller.ResolveContactLine(), Does.Contain("CLASSIFIED"));
    }
}