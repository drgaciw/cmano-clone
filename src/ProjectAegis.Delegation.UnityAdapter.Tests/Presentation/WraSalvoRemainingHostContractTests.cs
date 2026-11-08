using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class WraSalvoRemainingHostContractTests
{
    [Test]
    public void Right_unit_panel_binds_wra_salvo_from_bridge_projection()
    {
        var rightUnit = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(rightUnit, Does.Contain("ProjectSelectedWraSalvoRemaining"));
        Assert.That(rightUnit, Does.Contain("WraSalvoRemainingPanelBinder.BindRows"));
        Assert.That(rightUnit, Does.Contain("WraSalvoCueClasses"));
        Assert.That(rightUnit, Does.Contain("LastWraSalvoSurface"));
        Assert.That(rightUnit, Does.Contain("_wraSalvoSurface.IsExhausted"));
    }

    [Test]
    public void Bridge_host_exposes_wra_salvo_projection_helper()
    {
        var source = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(source, Does.Contain("ProjectSelectedWraSalvoRemaining"));
        Assert.That(source, Does.Contain("WraSalvoRemainingBinder.Bind"));
        Assert.That(source, Does.Contain("BuildSelectedLiveEngageContext"));
    }

    [Test]
    public void Unit_detail_layout_declares_wra_salvo_line()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "UnitDetail",
            "UnitDetailPanel.uxml");
        Assert.That(xml, Does.Contain("name=\"wra-salvo-line\""));
    }
}
