using System.Xml.Linq;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SliceAContactHostContractTests
{
    [Test]
    public void Committed_smoke_scene_wires_contact_panel_assets_and_host()
    {
        var scene = UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "Scenes", "DelegationSmoke.unity");
        Assert.That(scene, Does.Contain("guid: 10c00e65768e486e9660ac5b8c2ad6b4"));
        Assert.That(scene, Does.Contain("guid: 0e05122f633e467b8119e547b92d6d18"));
        Assert.That(scene, Does.Contain("guid: 19f4142fbf984db498e458e1f437a88f"));
    }

    [Test]
    public void Contact_panel_consumes_cached_frame_not_live_log()
    {
        var source = UiIaSourceReader.ReadRuntime("ContactDetailPanelHost.cs");
        Assert.That(source, Does.Contain("bridgeHost.LastSliceAContacts"));
        Assert.That(source, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(source, Does.Contain("ReferenceEquals"), "Unchanged frames must not rebuild panel text.");
    }

    [Test]
    public void Composition_root_publishes_frame_after_tick()
    {
        var source = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(source, Does.Contain("LastSliceAContacts = SliceAContactFrameBridge.Build(snapshot, Bridge, CatalogReader)"));
        Assert.That(source.IndexOf("LastSliceAContacts = SliceAContactFrameBridge.Build", StringComparison.Ordinal),
            Is.GreaterThan(source.IndexOf("var result = Bridge.Tick(snapshot, sink)", StringComparison.Ordinal)));
    }

    [Test]
    public void Contact_layout_has_scrollable_explanation_and_independent_authority()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder("unity", "ProjectAegis", "Assets", "UI", "ContactDetail", "ContactDetailPanel.uxml"));
        Assert.That(xml.Descendants().Any(e => e.Name.LocalName == "ScrollView"), Is.True);
        foreach (var name in new[] { "kill-chain-line", "sensor-shooter-line", "authority-line", "next-action-line" })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }
        Assert.That(xml.Descendants().Any(e => e.Name.LocalName == "Foldout"), Is.True);
    }
}
