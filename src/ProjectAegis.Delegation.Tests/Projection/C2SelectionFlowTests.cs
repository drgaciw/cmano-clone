using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2SelectionFlowTests
{
    [Test]
    public void Default_selection_syncs_map_and_oob_highlights()
    {
        var oob = new[] { new OobTreeEntry("u2", true), new OobTreeEntry("u1", true) };
        var selected = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        Assert.That(selected, Is.EqualTo("u1"));

        var symbols = MapPictureProjection.Project(oob, [], layoutSeed: 42);
        var mapState = MapPanelBinder.Bind(symbols, "baltic-patrol", selected, null);
        var oobState = OobTreePanelBinder.Bind(oob, selected);

        Assert.That(mapState.Symbols.Single(s => s.SymbolId == "u1").IsSelected, Is.True);
        Assert.That(oobState.UnitRows.Single(r => r.UnitId == "u1").IsSelected, Is.True);
    }

    [Test]
    public void Hostile_map_click_resolves_contact_summary_only()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)],
            42);

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("c1", symbols, out var contactId),
            Is.True);
        Assert.That(contactId, Is.EqualTo("c1"));

        var summary = ContactSummaryProjection.Project(
            contactId,
            [new ContactPictureEntry("c1", "t1", "u1", "CLASSIFIED", 5, 5.0)]);
        Assert.That(summary!.DisplayLine, Does.Contain("CLASSIFIED"));

        var mapState = MapPanelBinder.Bind(symbols, "baltic-patrol", null, contactId);
        Assert.That(mapState.Symbols.Single(s => s.SymbolId == "c1").IsSelected, Is.True);
    }

    [Test]
    public void Oob_row_click_syncs_map_highlight_for_friendly_unit()
    {
        var oob = new[] { new OobTreeEntry("u1", true), new OobTreeEntry("u2", true) };
        const string selected = "u2";
        var symbols = MapPictureProjection.Project(oob, [], layoutSeed: 7);
        var mapState = MapPanelBinder.Bind(symbols, "baltic-patrol", selected, null);
        var oobState = OobTreePanelBinder.Bind(oob, selected);

        Assert.That(mapState.Symbols.Single(s => s.SymbolId == "u2").IsSelected, Is.True);
        Assert.That(oobState.UnitRows.Single(r => r.UnitId == "u2").IsSelected, Is.True);
        Assert.That(mapState.Symbols.Single(s => s.SymbolId == "u1").IsSelected, Is.False);
    }

    [Test]
    public void Contact_list_row_selection_matches_map_contact_summary()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("c1", "t1", "u1", "IDENTIFIED", 5, 5.0),
            new ContactPictureEntry("c2", "t2", "u1", "CLASSIFIED", 3, 3.0),
        };
        var panel = SensorC2PanelBinder.Bind(
            new SensorC2Snapshot(contacts, 2, true, true, "c1", 2));
        var contactId = panel.ContactRows[1].ContactId;

        var summary = ContactSummaryProjection.Project(contactId, contacts);
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            contacts,
            layoutSeed: 42);

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(contactId, symbols, out var resolved),
            Is.True);
        Assert.That(resolved, Is.EqualTo(contactId));
        Assert.That(summary!.DisplayLine, Does.Contain("CLASSIFIED"));
    }
}