using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MagazineLoadoutHostContractTests
{
    [Test]
    public void Panel_host_consumes_cached_bridge_magazine_feed_and_fingerprint()
    {
        var source = UiIaSourceReader.ReadRuntime("MagazineLoadoutPanelHost.cs");
        Assert.That(source, Does.Contain("bridgeHost.LastMagazineLoadout"));
        Assert.That(source, Does.Contain("bridgeHost.HasMagazineLoadoutData"));
        Assert.That(source, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(source, Does.Contain("labels.Fingerprint"));
        Assert.That(source, Does.Contain("_lastFingerprint"));
        Assert.That(source, Does.Contain("MagazineLoadoutPanelBinder.Bind"));
        Assert.That(source, Does.Contain("labels.Fingerprint"));
        Assert.That(source, Does.Contain("labels.HasMagazineData"));
        Assert.That(source, Does.Contain("labels.EmptyStateLine"));
        Assert.That(source, Does.Contain("row.Remaining"));
        Assert.That(source, Does.Contain("row.Capacity"));
        Assert.That(source, Does.Contain("row.FillPct"));
    }

    [Test]
    public void Panel_binder_exposes_positional_props_for_list_bind()
    {
        var entries = MagazineLoadoutProjection.Project([
            ("cvn-1", "CVN", "vls", "aim120", "AIM-120", 6, 8),
        ]);
        var labels = MagazineLoadoutPanelBinder.Bind(
            MagazineLoadoutPresenter.Build(entries, hasMagazineData: true),
            roundsPerAirframe: 6);

        Assert.That(labels.HeaderLine, Does.Contain("MAGAZINE"));
        Assert.That(labels.HasMagazineData, Is.True);
        Assert.That(labels.Fingerprint, Does.StartWith("ml:r=1|"));
        Assert.That(labels.Rows[0].DisplayLine, Does.Contain("cvn-1"));
        Assert.That(labels.Rows[0].Remaining, Is.EqualTo(6));
        Assert.That(labels.Rows[0].Capacity, Is.EqualTo(8));
        Assert.That(labels.Rows[0].FillPct, Is.EqualTo(75.0).Within(0.01));
        Assert.That(labels.FeasibilityLine, Does.Contain("ARMABLE AIRFRAMES"));
    }

    [Test]
    public void Panel_layout_exposes_header_empty_list_and_feasibility()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MagazineLoadout", "MagazineLoadoutPanel.uxml"));
        foreach (var name in new[]
        {
            "magazine-loadout-root",
            "magazine-loadout-header",
            "magazine-loadout-empty",
            "magazine-loadout-list",
            "magazine-loadout-feasibility",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }
    }

    [Test]
    public void Scene_builder_wires_magazine_loadout_host()
    {
        var builder = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "Editor", "DelegationSmokeSceneBuilder.cs");
        Assert.That(builder, Does.Contain("MagazineLoadoutPanelHost"));
        Assert.That(builder, Does.Contain("\"MagazineLoadout\""));
        Assert.That(builder, Does.Contain("Assets/UI/MagazineLoadout/MagazineLoadoutPanel.uxml"));
    }
}
