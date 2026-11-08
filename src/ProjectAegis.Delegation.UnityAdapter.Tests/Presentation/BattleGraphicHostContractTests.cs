using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class BattleGraphicHostContractTests
{
    [Test]
    public void Battle_graphic_host_binds_projection_rows_and_cue_classes()
    {
        var source = UiIaSourceReader.ReadRuntime("BattleGraphicPanelHost.cs");
        Assert.That(source, Does.Contain("ProjectBattleGraphic"));
        Assert.That(source, Does.Contain("BattleGraphicPanelBinder.BindRows"));
        Assert.That(source, Does.Contain("BattleGraphicCueClasses"));
        Assert.That(source, Does.Contain("LastBattleGraphic"));
        Assert.That(source, Does.Not.Contain("RunTick"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("MapPlaceholderPanelHost"));
        Assert.That(source, Does.Not.Contain("GlobeMapProductHost"));
    }

    [Test]
    public void Bridge_host_exposes_battle_graphic_read_helper()
    {
        var source = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(source, Does.Contain("ProjectBattleGraphic"));
        Assert.That(source, Does.Contain("BattleGraphicBinder.Bind(DisplayCombatFrame"));
        Assert.That(source, Does.Contain("Projection read only"));
    }

    [Test]
    public void Engagement_layout_declares_battle_graphic_rows()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "BattleGraphic",
            "BattleGraphicPanel.uxml");
        Assert.That(xml, Does.Contain("name=\"battle-graphic-root\""));
        Assert.That(xml, Does.Contain("name=\"battle-graphic-summary\""));
        Assert.That(xml, Does.Contain("name=\"battle-graphic-line\""));
        Assert.That(xml, Does.Contain("name=\"battle-graphic-track\""));
        Assert.That(xml, Does.Contain("name=\"battle-graphic-trail\""));
        Assert.That(xml, Does.Contain("name=\"battle-graphic-detail\""));

        var uss = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "BattleGraphic",
            "BattleGraphicPanel.uss");
        Assert.That(uss, Does.Contain(".battle-cue--missile"));
        Assert.That(uss, Does.Contain(".battle-cue--gun"));
        Assert.That(uss, Does.Contain(".battle-cue--energy"));
        Assert.That(uss, Does.Contain("border-left-width"));
        Assert.That(uss, Does.Contain("border-bottom-width"));
        Assert.That(uss, Does.Contain("border-right-width"));
    }
}
