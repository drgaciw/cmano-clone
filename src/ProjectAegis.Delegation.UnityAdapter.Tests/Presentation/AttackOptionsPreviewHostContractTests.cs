using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class AttackOptionsPreviewHostContractTests
{
    [Test]
    public void Right_unit_panel_binds_attack_preview_from_engage_options_menu()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(source, Does.Contain("AttackOptionsPreviewBinder.Bind"));
        Assert.That(source, Does.Contain("AttackOptionsCueClasses"));
        Assert.That(source, Does.Contain("TrySelectAttackOption"));
        Assert.That(source, Does.Contain("RefreshAttackMenuButtons"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
    }

    [Test]
    public void Unit_detail_layout_declares_attack_option_buttons_and_blocked_cue()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "UnitDetail",
            "UnitDetailPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "UnitDetail",
            "UnitDetailPanel.uss");

        Assert.That(xml, Does.Contain("name=\"attack-options-line\""));
        Assert.That(xml, Does.Contain("name=\"attack-fire-single\""));
        Assert.That(xml, Does.Contain("name=\"attack-fire-salvo\""));
        Assert.That(xml, Does.Contain("name=\"attack-hold-fire\""));
        Assert.That(xml, Does.Contain("Fire 1 round"));
        Assert.That(xml, Does.Contain("attack-option-cue--empty"));
        Assert.That(uss, Does.Contain("attack-option-cue--blocked"));
        Assert.That(uss, Does.Contain("attack-option-cue--ready"));
    }
}
