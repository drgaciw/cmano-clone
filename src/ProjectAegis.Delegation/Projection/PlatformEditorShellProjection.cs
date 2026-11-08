namespace ProjectAegis.Delegation.Projection;

/// <summary>PE-UX-W4 / P-PE-04: active pane in the unified Platform Editor shell.</summary>
public enum PlatformEditorShellMode
{
    Catalog,
    Import,
}

/// <summary>Bindable shell chrome state — hosts remain thin binders.</summary>
public sealed record PlatformEditorShellState(
    PlatformEditorShellMode Mode,
    string ModeTitle,
    string RootUssClass,
    bool CatalogTabActive,
    bool ImportTabActive,
    bool CatalogContentVisible,
    bool ImportContentVisible,
    string? SelectedPlatformId,
    string StatusSummary);

/// <summary>Pure projection for Catalog | Import shell navigation (no write-gate).</summary>
public static class PlatformEditorShellProjection
{
    public static PlatformEditorShellState Bind(
        PlatformEditorShellMode mode = PlatformEditorShellMode.Catalog,
        string? selectedPlatformId = null,
        string statusSummary = "")
    {
        var isCatalog = mode == PlatformEditorShellMode.Catalog;
        return new PlatformEditorShellState(
            Mode: mode,
            ModeTitle: isCatalog ? "BROWSE CATALOG" : "IMPORT STAGING",
            RootUssClass: isCatalog ? "platform-editor-shell--browse" : "platform-editor-shell--import",
            CatalogTabActive: isCatalog,
            ImportTabActive: !isCatalog,
            CatalogContentVisible: isCatalog,
            ImportContentVisible: !isCatalog,
            SelectedPlatformId: selectedPlatformId,
            StatusSummary: statusSummary ?? string.Empty);
    }

    public static PlatformEditorShellState WithMode(
        PlatformEditorShellState state,
        PlatformEditorShellMode mode) =>
        Bind(mode, state.SelectedPlatformId, state.StatusSummary);

    public static PlatformEditorShellState WithSelection(
        PlatformEditorShellState state,
        string? selectedPlatformId) =>
        Bind(state.Mode, selectedPlatformId, state.StatusSummary);

    public static PlatformEditorShellState WithStatus(
        PlatformEditorShellState state,
        string statusSummary) =>
        Bind(state.Mode, state.SelectedPlatformId, statusSummary);

    public static PlatformEditorShellState CycleMode(PlatformEditorShellState state) =>
        WithMode(
            state,
            state.Mode == PlatformEditorShellMode.Catalog
                ? PlatformEditorShellMode.Import
                : PlatformEditorShellMode.Catalog);

    public static string TabUssClass(bool isActive) =>
        isActive
            ? "platform-editor-shell-tab platform-editor-shell-tab--active"
            : "platform-editor-shell-tab";
}
