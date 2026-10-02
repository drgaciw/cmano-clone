using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class DlzLiveSurfaceHostContractTests
{
    [Test]
    public void Contact_host_binds_dlz_live_surface_with_cue_classes()
    {
        var source = UiIaSourceReader.ReadRuntime("ContactDetailPanelHost.cs");
        Assert.That(source, Does.Contain("DlzLiveSurfaceBinder.BindContact"));
        Assert.That(source, Does.Contain("DlzLiveSurfacePanelBinder.BindRows"));
        Assert.That(source, Does.Contain("DlzCueClasses"));
        Assert.That(source, Does.Contain("LastDlzSurface"));
    }

    [Test]
    public void Right_unit_panel_binds_dlz_from_engage_preview()
    {
        var rightUnit = UiIaSourceReader.ReadRuntime("RightUnitPanelHost.cs");
        Assert.That(rightUnit, Does.Contain("ProjectSelectedEngagePreview"));
        Assert.That(rightUnit, Does.Contain("DlzLiveSurfaceBinder.BindFromEngagePreview"));
        Assert.That(rightUnit, Does.Contain("DlzCueClasses"));
    }

    [Test]
    public void Engage_explain_host_surfaces_dlz_cue_on_status_row()
    {
        var source = UiIaSourceReader.ReadRuntime("EngageExplainPanelHost.cs");
        Assert.That(source, Does.Contain("DlzLiveSurfaceBinder.BindFromEngagePreview"));
        Assert.That(source, Does.Contain("DlzCueClasses"));
    }

    [Test]
    public void Contact_layout_declares_dlz_line()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "ContactDetail",
            "ContactDetailPanel.uxml");
        Assert.That(xml, Does.Contain("name=\"dlz-line\""));
    }

    [Test]
    public void Unit_detail_layout_declares_dlz_line()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "UnitDetail",
            "UnitDetailPanel.uxml");
        Assert.That(xml, Does.Contain("name=\"dlz-line\""));
    }
}
