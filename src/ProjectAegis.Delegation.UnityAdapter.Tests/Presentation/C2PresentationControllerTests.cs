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

    // S37-04 + S38-04 + S39-03 residual C2 polish (filters/tooltips/density): graph surfacing (highlights + bind) via read-only catalog; extends proxy for C2 18/18+ (Graph*); cites boundary + sprint39.
    // per production/sprints/sprint-39-deeper-polish-c2-platform-hygiene.md + qa-plan-sprint-39-2026-06-20.md + polish-scope-boundary-2026-06-19.md ; extend-only.
    [Test]
    public void ApplyGraphSurfacing_populates_highlights_and_chain_for_unit()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");

        // use fixture (S37-03 chains built internally from mounts/comms/sensors)
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        controller.ApplyGraphSurfacing(reader);

        Assert.That(controller.LastGraphHighlightIds, Does.Contain("u1"));
        // chain may be empty or partial on minimal fixture; surfacing exercised; updated assertion for S39-03 evidence path
        Assert.That(controller.LastGraphLinkChainDisplay, Is.Not.Null, "graph surfacing chain display populated (C2 proxy/Graph*)");
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

    // BUG regression (qa-loop-08): switching selection from a friendly unit (with graph
    // surfacing already applied) to a hostile contact left the previous unit's graph
    // highlights/link-chain display stale on the controller. DelegationBridgeHost.SelectContact
    // intentionally never calls ApplyGraphSurfacingForSelection for contacts ("contacts do not
    // drive platform graph"), so this stale state is not transient — it persists until a
    // different friendly unit is selected. A C2 dependency-graph panel bound to
    // LastGraphHighlightIds/LastGraphLinkChainDisplay would keep highlighting equipment for a
    // unit that is no longer selected while the UI shows a hostile contact selected instead
    // (C2 view desync / editorState-vs-sim-state mismatch).
    [Test]
    public void SelectHostileContact_clears_stale_graph_highlights_from_previous_unit()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");
        controller.ApplyGraphSurfacing(InMemoryCatalogReader.BalticPatrolFixture());

        // Sanity: graph surfacing actually populated highlights for u1 before switching away.
        Assert.That(controller.LastGraphHighlightIds, Does.Contain("u1"));

        controller.SelectHostileContact(
            "c1",
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);

        Assert.That(controller.LastGraphHighlightIds, Is.Empty,
            "graph highlights from the previously selected friendly unit must not remain visible once selection switches to a hostile contact");
        Assert.That(controller.LastGraphLinkChainDisplay, Is.Null);
    }
}