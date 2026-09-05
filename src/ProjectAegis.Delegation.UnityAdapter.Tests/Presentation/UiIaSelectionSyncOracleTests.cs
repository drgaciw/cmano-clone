namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class UiIaSelectionSyncOracleTests
{
    [Test]
    public void Classify_binders_share_one_hostile_contact_id_across_map_oob_summary_and_sensor()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var oob = new[] { new OobTreeEntry("u1", true) };
        var defaultUnit = C2SelectionResolver.ResolveDefaultFriendlyUnit(oob);
        var symbols = MapPictureProjection.Project(oob, result.SensorC2.Contacts, layoutSeed: 7);

        var mapDefault = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", defaultUnit, null);
        var oobDefault = OobTreePanelBinder.Bind(oob, defaultUnit);
        Assert.That(mapDefault.Symbols.Single(s => s.SymbolId == defaultUnit).IsSelected, Is.True);
        Assert.That(oobDefault.UnitRows.Single(r => r.UnitId == defaultUnit).IsSelected, Is.True);

        var hostile = symbols.First(s => s.Affiliation == "Hostile");
        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(hostile.SymbolId, symbols, out var contactId),
            Is.True);

        var summary = ContactSummaryProjection.Project(contactId, result.SensorC2.Contacts);
        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.DisplayLine, Does.Contain("CONTACT"));

        var mapContact = MapPanelBinder.Bind(symbols, "baltic-patrol-classify", null, contactId);
        Assert.That(mapContact.Symbols.Single(s => s.SymbolId == contactId).IsSelected, Is.True);

        var sensor = SensorC2PanelBinder.Bind(result.SensorC2);
        Assert.That(sensor.ContactRows.Any(r => r.ContactId == contactId), Is.True);
    }

    [Test]
    public void Selection_hosts_bind_bridge_ids_and_do_not_keep_a_second_selection_store()
    {
        var map = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(map, Does.Contain("SelectedContactId"));
        Assert.That(map, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(map, Does.Not.Contain("DelegationBridge.Tick"));

        var oob = UiIaSourceReader.ReadRuntime("OobTreePanelHost.cs");
        Assert.That(oob, Does.Contain("OobTreePanelBinder.Bind"));
        Assert.That(oob, Does.Contain("SelectedUnitId"));
        Assert.That(oob, Does.Not.Contain("private string? _selected"));

        var contact = UiIaSourceReader.ReadRuntime("ContactDetailPanelHost.cs");
        Assert.That(contact, Does.Contain("bridgeHost.SelectedContactId"));
        Assert.That(contact, Does.Not.Contain("private string? _selected"));

        var unit = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(unit, Does.Contain("UnitDetailPanelBinder.Bind"));
        Assert.That(unit, Does.Contain("LastUnitDetail"));
        Assert.That(unit, Does.Not.Contain("private string? _selected"));

        var sensor = UiIaSourceReader.ReadRuntime("SensorC2PanelHost.cs");
        Assert.That(sensor, Does.Contain("LastSensorC2"));
        Assert.That(sensor, Does.Contain("SensorC2Bridge.BindPanel"));
        Assert.That(sensor, Does.Not.Contain("private string? _selected"));

        var drawer = UiIaSourceReader.ReadRuntime("C2LeftDrawerPanelHost.cs");
        Assert.That(drawer, Does.Contain("SelectContact"));
        Assert.That(drawer, Does.Contain("SelectedUnitId"));
        Assert.That(drawer, Does.Contain("selectionType = SelectionType.Single"));
        Assert.That(drawer, Does.Contain("selectionChanged += OnOobSelectionChanged"));
        Assert.That(drawer, Does.Not.Contain("RegisterCallback<ClickEvent>(OnOobRowClicked)"));
        Assert.That(drawer, Does.Not.Contain("CatalogWriteGate"));
    }
}
