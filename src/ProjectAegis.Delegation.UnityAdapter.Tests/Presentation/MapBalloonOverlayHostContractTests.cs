using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MapBalloonOverlayHostContractTests
{
    [Test]
    public void MapBalloonOverlayPanelHost_is_thin_companion_overlay_with_ignore_picking_and_no_sim_authority()
    {
        var source = UiIaSourceReader.ReadRuntime("MapBalloonOverlayPanelHost.cs");

        Assert.That(source, Does.Contain("DefaultOverlaySortingOrder = 7"));
        Assert.That(source, Does.Contain("MapBalloonOverlayProjection.Project"));
        Assert.That(source, Does.Contain("PickingMode.Ignore"));
        Assert.That(source, Does.Not.Contain("PickingMode.Position"));
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("SimulationSession"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("Resources.Load"));
        Assert.That(source, Does.Not.Contain("AppendPlayerOrder"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
    }

    [Test]
    public void MapBalloonOverlayPanel_uxml_and_uss_declare_bounds_elements_and_ignore_picking()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MapBalloonOverlay", "MapBalloonOverlayPanel.uxml"));
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MapBalloonOverlay", "MapBalloonOverlayPanel.uss");

        foreach (var name in new[]
        {
            MapBalloonOverlayProjection.RootElementName,
            MapBalloonOverlayProjection.UnanchoredStripElementName,
            MapBalloonOverlayProjection.CanvasElementName,
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }

        Assert.That(xml.Root, Is.Not.Null);
        Assert.That(xml.Root!.Descendants().All(e => (string?)e.Attribute("picking-mode") == "Ignore"), Is.True,
            "Every UI element in MapBalloonOverlayPanel.uxml must specify picking-mode=\"Ignore\".");

        Assert.That(uss, Does.Contain("left: 260px;"));
        Assert.That(uss, Does.Contain("right: 300px;"));
        Assert.That(uss, Does.Contain("top: 56px;"));
        Assert.That(uss, Does.Contain("bottom: 0;"));
        Assert.That(uss, Does.Not.Contain("picking-mode:"));
        Assert.That(uss, Does.Not.Contain("-unity-font: resource("));
        Assert.That(uss, Does.Contain("." + MapBalloonOverlayProjection.PauseBalloonCssClass));
        Assert.That(uss, Does.Contain("." + MapBalloonOverlayProjection.AlertBalloonCssClass));
    }

    [Test]
    public void Hard_no_map_hosts_remain_untouched_by_DRG408()
    {
        var mapPlaceholder = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        var globeMap = UiIaSourceReader.ReadRuntime("GlobeMapProductHost.cs");

        Assert.That(mapPlaceholder, Does.Not.Contain("MapBalloonOverlay"));
        Assert.That(globeMap, Does.Not.Contain("MapBalloonOverlay"));
    }
}
