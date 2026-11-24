using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class CoveragePostureHostContractTests
{
    [Test]
    public void CoveragePostureOverlayPanelHost_is_thin_passive_overlay_with_sorting_order_8_and_no_sim_authority()
    {
        var source = UiIaSourceReader.ReadRuntime("CoveragePostureOverlayPanelHost.cs");

        Assert.That(source, Does.Contain("DefaultOverlaySortingOrder = 8"));
        Assert.That(source, Does.Contain("PickingMode.Ignore"));
        Assert.That(source, Does.Contain("CoveragePostureProjection"));
        Assert.That(source, Does.Contain("CoverageMapScaleEvaluator"));
        Assert.That(source, Does.Contain("[DisallowMultipleComponent]"));
        Assert.That(source, Does.Contain("[RequireComponent(typeof(UIDocument))]"));
        Assert.That(source, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"));
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
        Assert.That(source, Does.Not.Contain("ADR-018"));

        // Verify pure C# types compile and live in UnityAdapter
        Assert.That(typeof(CoveragePostureProjection).FullName, Is.Not.Null);
        Assert.That(typeof(CoverageMapScaleEvaluator).FullName, Is.Not.Null);
    }

    [Test]
    public void CoveragePostureOverlayPanel_uxml_and_uss_declare_required_elements_bounds_and_ignore_picking()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "CoveragePostureOverlay", "CoveragePostureOverlayPanel.uxml"));
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "CoveragePostureOverlay", "CoveragePostureOverlayPanel.uss");

        foreach (var name in new[]
        {
            "coverage-posture-overlay-root",
            "coverage-posture-card",
            "coverage-posture-header-strip",
            "coverage-posture-scale-badge",
            "coverage-posture-authority-badge",
            "coverage-posture-summary-label",
            "coverage-posture-layers-label",
            "coverage-posture-gaps-label",
            "coverage-posture-diff-box",
            "coverage-posture-diff-headline",
            "coverage-posture-diff-detail",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }

        Assert.That(xml.Root, Is.Not.Null);
        Assert.That(xml.Root!.Descendants().All(e => (string?)e.Attribute("picking-mode") == "Ignore"), Is.True,
            "Every UI element in CoveragePostureOverlayPanel.uxml must specify picking-mode=\"Ignore\".");

        Assert.That(uss, Does.Contain("left: 260px;"));
        Assert.That(uss, Does.Contain("right: 300px;"));
        Assert.That(uss, Does.Contain("top: 56px;"));
        Assert.That(uss, Does.Contain("bottom: 0;"));
        Assert.That(uss, Does.Not.Contain("picking-mode:"));
        Assert.That(uss, Does.Not.Contain("-unity-font: resource("));
        Assert.That(uss, Does.Contain(".coverage-posture--nominal"));
        Assert.That(uss, Does.Contain(".coverage-posture--degraded"));
        Assert.That(uss, Does.Contain(".coverage-posture--critical"));
        Assert.That(uss, Does.Contain(".coverage-posture--unknown"));
    }

    [Test]
    public void Hard_no_map_hosts_remain_untouched_by_DRG192()
    {
        var mapPlaceholder = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        var globeMap = UiIaSourceReader.ReadRuntime("GlobeMapProductHost.cs");

        Assert.That(mapPlaceholder, Does.Not.Contain("CoveragePostureOverlay"));
        Assert.That(globeMap, Does.Not.Contain("CoveragePostureOverlay"));
    }

    [Test]
    public void CoverageMapView_preserves_forbidden_substring_and_c_sharp_10_compatibility()
    {
        var source = UiIaSourceReader.ReadRuntime("CoverageMapView.cs");

        Assert.That(source, Does.Not.Contain("radius"));
        Assert.That(source, Does.Contain("coverage.Status == CoverageStatus.Gap ? 4 : 1"));
        Assert.That(source, Does.Contain("gap ? \"GAP\" : \"COVERED\""));
        Assert.That(source, Does.Contain("GeometryChangedEvent"));
        Assert.That(source, Does.Contain("MaxSegments = 2048"));
        Assert.That(source, Does.Contain("CoverageMapScaleEvaluator"));
        Assert.That(source, Does.Contain("CoveragePostureSnapshot"));
    }
}
