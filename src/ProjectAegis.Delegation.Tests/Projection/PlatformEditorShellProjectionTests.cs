using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PE-UX-W4: Unified Platform Editor shell state (P-PE-04).</summary>
[TestFixture]
public sealed class PlatformEditorShellProjectionTests
{
    [Test]
    public void Bind_default_is_catalog_browse_mode()
    {
        var state = PlatformEditorShellProjection.Bind();

        Assert.That(state.Mode, Is.EqualTo(PlatformEditorShellMode.Catalog));
        Assert.That(state.ModeTitle, Is.EqualTo("BROWSE CATALOG"));
        Assert.That(state.RootUssClass, Is.EqualTo("platform-editor-shell--browse"));
        Assert.That(state.CatalogTabActive, Is.True);
        Assert.That(state.ImportTabActive, Is.False);
        Assert.That(state.CatalogContentVisible, Is.True);
        Assert.That(state.ImportContentVisible, Is.False);
    }

    [Test]
    public void Bind_import_mode_swaps_visibility_and_title()
    {
        var state = PlatformEditorShellProjection.Bind(
            PlatformEditorShellMode.Import,
            selectedPlatformId: "u1",
            statusSummary: "Pending: 2 changed");

        Assert.That(state.Mode, Is.EqualTo(PlatformEditorShellMode.Import));
        Assert.That(state.ModeTitle, Is.EqualTo("IMPORT STAGING"));
        Assert.That(state.RootUssClass, Is.EqualTo("platform-editor-shell--import"));
        Assert.That(state.CatalogTabActive, Is.False);
        Assert.That(state.ImportTabActive, Is.True);
        Assert.That(state.CatalogContentVisible, Is.False);
        Assert.That(state.ImportContentVisible, Is.True);
        Assert.That(state.SelectedPlatformId, Is.EqualTo("u1"));
        Assert.That(state.StatusSummary, Is.EqualTo("Pending: 2 changed"));
    }

    [Test]
    public void WithMode_preserves_selection_and_status()
    {
        var browse = PlatformEditorShellProjection.Bind(
            PlatformEditorShellMode.Catalog,
            selectedPlatformId: "hostile-1",
            statusSummary: "Health: OK");

        var import = PlatformEditorShellProjection.WithMode(browse, PlatformEditorShellMode.Import);

        Assert.That(import.Mode, Is.EqualTo(PlatformEditorShellMode.Import));
        Assert.That(import.SelectedPlatformId, Is.EqualTo("hostile-1"));
        Assert.That(import.StatusSummary, Is.EqualTo("Health: OK"));
        Assert.That(import.ImportContentVisible, Is.True);
    }

    [Test]
    public void CycleMode_toggles_catalog_and_import()
    {
        var catalog = PlatformEditorShellProjection.Bind(PlatformEditorShellMode.Catalog);
        var import = PlatformEditorShellProjection.CycleMode(catalog);
        var back = PlatformEditorShellProjection.CycleMode(import);

        Assert.That(import.Mode, Is.EqualTo(PlatformEditorShellMode.Import));
        Assert.That(back.Mode, Is.EqualTo(PlatformEditorShellMode.Catalog));
    }

    [Test]
    public void TabUssClass_marks_active_tab()
    {
        Assert.That(
            PlatformEditorShellProjection.TabUssClass(isActive: true),
            Is.EqualTo("platform-editor-shell-tab platform-editor-shell-tab--active"));
        Assert.That(
            PlatformEditorShellProjection.TabUssClass(isActive: false),
            Is.EqualTo("platform-editor-shell-tab"));
    }
}
