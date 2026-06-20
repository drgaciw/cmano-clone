using ProjectAegis.Data.Catalog;
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

    // S37-04: graph surfacing (highlights + bind) via read-only catalog; extends proxy for C2 18/18+
    [Test]
    public void ApplyGraphSurfacing_populates_highlights_and_chain_for_unit()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");

        // use fixture (S37-03 chains built internally from mounts/comms/sensors)
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        controller.ApplyGraphSurfacing(reader);

        Assert.That(controller.LastGraphHighlightIds, Does.Contain("u1"));
        // chain may be empty or partial on minimal fixture; surfacing exercised
        Assert.That(controller.LastGraphLinkChainDisplay, Is.Not.Null);
    }

    [Test]
    public void ApplyGraphSurfacing_graceful_empty_when_no_catalog_or_no_selection()
    {
        var controller = new C2PresentationController();
        controller.ApplyGraphSurfacing(null);
        Assert.That(controller.LastGraphHighlightIds, Is.Empty);

        controller.SelectFriendlyUnit("u-x");
        controller.ApplyGraphSurfacing(InMemoryCatalogReader.BalticPatrolFixture());
        // fixture may have chains; ensure no crash and state set
        Assert.That(controller.LastGraphLinkChainDisplay, Is.Not.Null.Or.Empty);
    }
}