using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>PE-UX-W4 headless proxy: shell mode preserves selection/status across Catalog↔Import.</summary>
[TestFixture]
public sealed class PlatformEditorShellHostTests
{
    [Test]
    public void Shell_mode_switch_preserves_selection_and_status_for_staging_continuity()
    {
        var browse = PlatformEditorShellProjection.Bind(
            PlatformEditorShellMode.Catalog,
            selectedPlatformId: "u1",
            statusSummary: "Pending: 3 changed");

        var import = PlatformEditorShellProjection.WithMode(browse, PlatformEditorShellMode.Import);
        var back = PlatformEditorShellProjection.WithMode(import, PlatformEditorShellMode.Catalog);

        Assert.That(import.ImportContentVisible, Is.True);
        Assert.That(import.CatalogContentVisible, Is.False);
        Assert.That(import.SelectedPlatformId, Is.EqualTo("u1"));
        Assert.That(import.StatusSummary, Is.EqualTo("Pending: 3 changed"));
        Assert.That(back.CatalogContentVisible, Is.True);
        Assert.That(back.SelectedPlatformId, Is.EqualTo("u1"));
    }

    [Test]
    public void Health_strip_formats_attention_when_pending_or_blocked()
    {
        Assert.That(
            PlatformCatalogHealthProjection.Format(0, 0, 10),
            Does.Contain("OK"));
        Assert.That(
            PlatformCatalogHealthProjection.Format(1, 4, 10),
            Does.Contain("ATTENTION"));
    }

    [Test]
    public void Diff_display_line_includes_text_tag_for_a11y()
    {
        var row = new PlatformImportStagingRow(
            "Platforms",
            1,
            "DAMAGE · MaxHp",
            PlatformImportStagingSection.Damage,
            PlatformImportStagingDiffKind.Changed,
            "platform-import-diff-row--changed");

        Assert.That(
            PlatformImportStagingProjection.FormatDisplayLine(row),
            Is.EqualTo("[CHANGED] DAMAGE · MaxHp"));
    }
}
