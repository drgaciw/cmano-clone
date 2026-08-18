namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaPlanningGateOracleTests
{
    [Test]
    public void Map_placeholder_binds_planning_dim_class()
    {
        var host = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("map-placeholder-panel--planning-dimmed"));
    }

    [Test]
    public void Left_drawer_binds_planning_readonly_class()
    {
        var host = UiIaSourceReader.ReadRuntime("C2LeftDrawerPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDrawerReadOnly"));
        Assert.That(host, Does.Contain("c2-drawer-panel--planning-readonly"));
    }

    [Test]
    public void Menu_host_applies_planning_readonly_and_guards_clicks()
    {
        var host = UiIaSourceReader.ReadRuntime("C2MenuPanelHost.cs");
        Assert.That(host, Does.Contain("C2PlanningChromeProjection.Project"));
        Assert.That(host, Does.Contain("IsDrawerReadOnly"));
        Assert.That(host, Does.Contain("c2-menu-panel--planning-readonly"));
        Assert.That(host, Does.Contain("OnLayerRowClicked"));
        Assert.That(host, Does.Contain("OnCycleUnitClicked"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));

        var uss = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "C2Menu",
            "C2MenuPanel.uss");
        Assert.That(uss, Does.Contain(".c2-menu-panel--planning-readonly"));
    }
}
