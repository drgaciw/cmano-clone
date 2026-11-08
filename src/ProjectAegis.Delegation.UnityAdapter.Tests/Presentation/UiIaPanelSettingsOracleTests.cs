namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaPanelSettingsOracleTests
{
    [Test]
    public void Bootstrap_assigns_PanelSettings_and_does_not_tick_sim()
    {
        var text = UiIaSourceReader.ReadRuntime("UiDocumentPanelSettingsBootstrap.cs");
        Assert.That(text, Does.Contain("EnsureDocument"));
        Assert.That(text, Does.Contain("SharedRuntimeSettings"));
        Assert.That(text, Does.Contain("panelSettings"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("BeginExecution"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(text, Does.Not.Contain("SimulationSession"));
    }

    [Test]
    public void Scene_builder_has_FixPanelSettings_path()
    {
        var text = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        Assert.That(text, Does.Contain("FixPanelSettings"));
        Assert.That(text, Does.Contain("EnsurePanelSettingsAsset"));
        Assert.That(text, Does.Contain("DefaultPanelSettingsAssetPath"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Participating_hosts_call_EnsureDocument()
    {
        string[] hosts =
        {
            "MapPlaceholderPanelHost.cs",
            "OobTreePanelHost.cs",
            "ContactDetailPanelHost.cs",
            "RightUnitPanelHost.cs",
            "SensorC2PanelHost.cs",
            "C2LeftDrawerPanelHost.cs",
            "C2TopBarPanelHost.cs",
            "C2MenuPanelHost.cs",
        };

        foreach (var host in hosts)
        {
            var text = UiIaSourceReader.ReadRuntime(host);
            Assert.That(text, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"), host);
        }
    }
}
