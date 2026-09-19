using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-190 — C2 top-bar COMMS / network-health host wiring contracts (headless source asserts).</summary>
[TestFixture]
public sealed class C2NetworkHealthHostContractTests
{
    [Test]
    public void Top_bar_host_binds_cached_network_health_not_live_projection()
    {
        var host = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");
        Assert.That(host, Does.Contain("bridgeHost.LastNetworkHealth"));
        Assert.That(host, Does.Contain("C2NetworkHealthPanelBinder.Bind"));
        Assert.That(host, Does.Contain("C2NetworkHealthPresenter.Build"));
        Assert.That(host, Does.Contain("C2NetworkHealthFingerprint.Compute"));
        Assert.That(host, Does.Contain("AddToClassList(_networkLabels.CssClass)"));
        Assert.That(host, Does.Not.Contain("C2NetworkHealthProjector.Project"));
        Assert.That(host, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Bridge_host_exposes_LastNetworkHealth_on_presentation_feed()
    {
        var feed = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation.UnityAdapter", "Bridge", "IC2PresentationFeed.cs");
        var host = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(feed, Does.Contain("LastNetworkHealth"));
        Assert.That(host, Does.Contain("LastNetworkHealth"));
        Assert.That(host, Does.Contain("C2NetworkHealthBridge.Build"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Top_bar_uxml_declares_network_health_bind_targets()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "TopBar", "C2TopBarPanel.uxml"));
        foreach (var name in new[]
        {
            "c2-topbar-comms-legend",
            "comms-network-advisory-badge",
            "comms-network-health-label",
            "comms-network-node-label",
            "comms-network-detail-label",
            "comms-network-contributors-label",
            "comms-network-lost-paths-label",
            "comms-network-next-label",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }
    }

    [Test]
    public void Top_bar_uss_declares_network_health_css_modifiers()
    {
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "TopBar", "C2TopBarPanel.uss");
        Assert.That(uss, Does.Contain(".c2-comms--healthy"));
        Assert.That(uss, Does.Contain(".c2-comms--degraded"));
        Assert.That(uss, Does.Contain(".c2-comms--partitioned"));
    }

    [Test]
    public void Panel_binder_exposes_summary_and_css_for_host_bind()
    {
        var labels = C2NetworkHealthPanelBinder.Bind(C2NetworkHealthPresentation.Empty);
        Assert.That(labels.SummaryLine, Does.Contain("Network"));
        Assert.That(labels.CssClass, Is.EqualTo("c2-comms--unknown"));
        Assert.That(labels.LinkCountLine, Does.Contain("Links"));
        Assert.That(labels.NextActionLine, Does.Contain("Obtain"));
    }

    [Test]
    public void Top_bar_host_binds_advisory_node_and_count_labels()
    {
        var host = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");
        Assert.That(host, Does.Contain("_networkLabels.AdvisoryBadge"));
        Assert.That(host, Does.Contain("_networkLabels.CommsNodeLine"));
        Assert.That(host, Does.Contain("_networkLabels.ContributorCountLine"));
        Assert.That(host, Does.Contain("_networkLabels.LostPathCountLine"));
        Assert.That(host, Does.Contain("DisplayStyle.None"));
    }

    [Test]
    public void Panel_binder_exposes_advisory_node_and_count_lines_for_host_bind()
    {
        var labels = C2NetworkHealthPanelBinder.Bind(C2NetworkHealthPresentation.Empty);
        Assert.That(labels.AdvisoryBadge, Does.Contain("never fabricated"));
        Assert.That(labels.CommsNodeLine, Does.Contain("Node"));
        Assert.That(labels.ContributorCountLine, Does.Contain("contributors"));
        Assert.That(labels.LostPathCountLine, Does.Contain("Lost paths"));
    }
}
