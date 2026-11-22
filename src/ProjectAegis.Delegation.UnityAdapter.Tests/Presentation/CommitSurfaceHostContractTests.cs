using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// DRG-370 &amp; DRG-351: RightUnitPanelHost live commit-strip, commit-bark, and runtime tooltip
/// wiring contracts (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
[TestFixture]
public sealed class CommitSurfaceHostContractTests
{
    [Test]
    public void Right_unit_panel_wires_commit_strip_bark_and_runtime_tooltip_read_only()
    {
        var source = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");

        Assert.That(source, Does.Contain("CommitConstraintStripBinder.BindRows"));
        Assert.That(source, Does.Contain("CommitRefusalBarkBinder.BindRow"));
        Assert.That(source, Does.Contain("CommitRefusalBarkBinder.Refused"));
        Assert.That(source, Does.Contain("CommitConstraintStripBinder.ConstraintElementName"));
        Assert.That(source, Does.Contain("CommitConstraintStripBinder.CostElementName"));
        Assert.That(source, Does.Contain("CommitRefusalBarkBinder.BarkElementName"));
        Assert.That(source, Does.Contain("RuntimeTooltipPresenter"));
        Assert.That(source, Does.Contain("LastCommitStrip"));
        Assert.That(source, Does.Contain("LastCommitBark"));
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
    }

    [Test]
    public void Unit_detail_layout_declares_commit_strip_bark_and_tooltip_styles()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "UnitDetail", "UnitDetailPanel.uss");

        Assert.That(xml, Does.Contain($"name=\"{CommitConstraintStripBinder.ConstraintElementName}\""));
        Assert.That(xml, Does.Contain($"name=\"{CommitConstraintStripBinder.CostElementName}\""));
        Assert.That(xml, Does.Contain($"name=\"{CommitRefusalBarkBinder.BarkElementName}\""));
        Assert.That(uss, Does.Contain("." + CommitConstraintStripCueClasses.Empty));
        Assert.That(uss, Does.Contain("." + CommitConstraintStripCueClasses.Ready));
        Assert.That(uss, Does.Contain("." + CommitConstraintStripCueClasses.Blocked));
        Assert.That(uss, Does.Contain("." + CommitRefusalBarkCueClasses.Refused));
        Assert.That(uss, Does.Contain("." + CommitRefusalBarkCueClasses.Dropped));
        Assert.That(uss, Does.Contain(".unit-detail-runtime-tooltip"));
    }
}
