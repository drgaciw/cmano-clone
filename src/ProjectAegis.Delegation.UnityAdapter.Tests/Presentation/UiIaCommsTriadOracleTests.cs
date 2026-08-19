namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

using NUnit.Framework;

[TestFixture]
public sealed class UiIaCommsTriadOracleTests
{
    [Test]
    public void Map_host_binds_LastCommsState_and_does_not_reproject()
    {
        var text = UiIaSourceReader.ReadRuntime("MapPlaceholderPanelHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void Top_bar_binds_LastCommsState_and_does_not_reproject()
    {
        var text = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Contain("TopBarLabel"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void Bridge_host_exposes_LastCommsState_on_the_presentation_feed()
    {
        var text = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(text, Does.Contain("LastCommsState"));
        Assert.That(text, Does.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
    }
}
