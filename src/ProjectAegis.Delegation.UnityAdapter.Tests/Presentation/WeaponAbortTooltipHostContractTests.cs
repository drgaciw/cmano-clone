using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class WeaponAbortTooltipHostContractTests
{
    [Test]
    public void Right_unit_panel_binds_weapon_abort_tooltip_from_explain_and_combat_detail()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(source, Does.Contain("WeaponAbortTooltipBinder.Bind"));
        Assert.That(source, Does.Contain("EngageExplainProjection.Project"));
        Assert.That(source, Does.Contain("ProjectCombatDetail"));
        Assert.That(source, Does.Contain("WeaponAbortTooltipBinder.AppliesTo"));
        Assert.That(source, Does.Contain("WeaponAbortTooltipBinder.DeniedRowClass"));
        Assert.That(source, Does.Contain("AttackOptionsPreviewBinder.Bind"));
        Assert.That(source, Does.Contain("RefreshAttackMenuButtons"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
    }

    [Test]
    public void Unit_detail_layout_declares_focusable_weapon_rows()
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

        Assert.That(xml, Does.Contain("name=\"weapon-row-fire-single\""));
        Assert.That(xml, Does.Contain("name=\"weapon-row-fire-salvo\""));
        Assert.That(xml, Does.Contain("name=\"attack-fire-single\""));
        Assert.That(xml, Does.Contain("name=\"attack-fire-salvo\""));
        Assert.That(xml, Does.Contain("name=\"attack-hold-fire\""));
        Assert.That(xml, Does.Contain("focusable=\"true\""));
        Assert.That(uss, Does.Contain("unit-detail-weapon-row--denied"));
        Assert.That(uss, Does.Contain(".unit-detail-weapon-row:focus"));
        Assert.That(uss, Does.Contain("attack-option-cue--blocked"));
    }
}
