using System.Xml.Linq;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class UnitControlHostContractTests
{
    [Test]
    public void Right_unit_panel_wires_control_handoff_via_projection_and_bridge_host_without_sim_or_orchestrator()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        var bridgeHostSource = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");

        Assert.That(bridgeHostSource, Does.Contain("RightUnitControlProjection.Project"));
        Assert.That(source, Does.Contain("ProjectSelectedControlHandoff"));
        Assert.That(source, Does.Contain("RightUnitControlState"));
        Assert.That(source, Does.Contain("TryTakeControlOfSelected"));
        Assert.That(source, Does.Contain("TryReturnToAgentOfSelected"));
        Assert.That(source, Does.Contain("TryPauseAgentOfSelected"));
        Assert.That(source, Does.Contain("TryResumeAgentOfSelected"));
        Assert.That(source, Does.Contain("LastControlState"));
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(source, Does.Not.Contain("AttachReplayViewer ="));
        Assert.That(source, Does.Not.Contain("private string? _selected"));
    }

    [Test]
    public void Unit_detail_layout_declares_control_handoff_elements_and_styles()
    {
        var xmlText = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uxml");
        var ussText = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uss");
        var xml = XDocument.Parse(xmlText);

        foreach (var name in new[]
        {
            "unit-control-status",
            "unit-take-control",
            "unit-return-to-agent",
            "unit-pause-agent",
            "unit-resume-agent",
            "unit-control-feedback",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }

        Assert.That(ussText, Does.Contain(".unit-detail-control-actions"));
        Assert.That(ussText, Does.Contain(".unit-detail-control-btn"));
    }
}
