namespace ProjectAegis.Delegation.Projection;

using Data.Catalog;
using Data.Platform;
using Data.Scenario.Authoring;

/// <summary>Bindable "which catalog am I editing against" chip for ME and PE chrome.</summary>
public sealed record EditorCatalogBindingState(
    EditorSurface Surface,
    string DbRef,
    string SnapshotId,
    string TlBranch,
    bool IsBound,
    bool IsMismatch,
    string BindingLabel,
    string DetailText,
    string UssClass);

/// <summary>
/// S125-07 / AUTH-12: exposes the catalog DB ref / snapshot id an editor session is bound to (scenario
/// <c>metadata.dbRef</c>/<c>dbSnapshotId</c>/<c>tlBranch</c>, workbook <c>_Meta.SourceSnapshotId</c>) and
/// flags unbound or unresolved bindings instead of hiding them. Read-only; no catalog writes.
/// </summary>
public static class EditorCatalogBindingProjection
{
    private const string Unset = "—";

    /// <summary>Mission Editor binding; resolves <c>dbRef</c> through the catalog when one is supplied.</summary>
    public static EditorCatalogBindingState ForScenario(ScenarioMetadataDto? metadata, ICatalogReader? catalog = null)
    {
        var dbRef = metadata?.DbRef?.Trim() ?? string.Empty;
        var declaredSnapshot = metadata?.DbSnapshotId?.Trim() ?? string.Empty;
        var tl = metadata?.TlBranch?.Trim() ?? string.Empty;

        if (dbRef.Length == 0 && declaredSnapshot.Length == 0)
        {
            return Build(
                EditorSurface.MissionEditor,
                string.Empty,
                string.Empty,
                tl,
                isBound: false,
                isMismatch: false,
                "DB: unbound",
                "Scenario metadata has no dbRef or dbSnapshotId; validation resolves the catalog from tlBranch only.");
        }

        var snapshot = declaredSnapshot;
        var mismatch = false;
        var detail = "Bound from scenario metadata.";
        if (catalog is not null && dbRef.Length > 0)
        {
            if (!catalog.TryResolveDbRef(dbRef, out var resolved))
            {
                mismatch = true;
                detail = $"dbRef '{dbRef}' does not resolve in catalog '{catalog.LayerVersion}' (DB_MISMATCH).";
            }
            else if (snapshot.Length > 0 && !string.Equals(snapshot, resolved, StringComparison.Ordinal))
            {
                mismatch = true;
                detail = $"metadata dbSnapshotId '{snapshot}' differs from catalog-resolved snapshot '{resolved}'.";
            }
            else
            {
                snapshot = resolved;
                detail = $"dbRef resolved by catalog '{catalog.LayerVersion}'.";
            }
        }

        return Build(EditorSurface.MissionEditor, dbRef, snapshot, tl, isBound: true, mismatch, Label(dbRef, snapshot, tl, mismatch), detail);
    }

    /// <summary>Platform Editor binding read from a workbook's <c>_Meta</c> sheet.</summary>
    public static EditorCatalogBindingState ForWorkbook(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var snapshot = Meta(workbook, "SourceSnapshotId");
        var dbVersion = Meta(workbook, "DbVersion");
        var tl = Meta(workbook, "TlTier");
        if (snapshot.Length == 0)
        {
            return Build(
                EditorSurface.PlatformEditor,
                dbVersion,
                string.Empty,
                tl,
                isBound: false,
                isMismatch: false,
                "DB: unbound",
                "Workbook _Meta has no SourceSnapshotId; import cannot diff or stage.");
        }

        return Build(
            EditorSurface.PlatformEditor,
            dbVersion,
            snapshot,
            tl,
            isBound: true,
            isMismatch: false,
            Label(dbVersion, snapshot, tl, mismatch: false),
            $"Workbook exported from snapshot '{snapshot}'.");
    }

    /// <summary>Platform Editor binding after planning an import: unresolved snapshots are a visible mismatch.</summary>
    public static EditorCatalogBindingState ForImportPlan(PlatformImportPlan plan, PlatformWorkbook edited)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var state = ForWorkbook(edited);
        if (!state.IsBound || plan.SnapshotResolved)
        {
            return state;
        }

        return Build(
            EditorSurface.PlatformEditor,
            state.DbRef,
            state.SnapshotId,
            state.TlBranch,
            isBound: true,
            isMismatch: true,
            Label(state.DbRef, state.SnapshotId, state.TlBranch, mismatch: true),
            $"Snapshot '{state.SnapshotId}' did not resolve against the live catalog; nothing will be staged.");
    }

    private static EditorCatalogBindingState Build(
        EditorSurface surface,
        string dbRef,
        string snapshot,
        string tl,
        bool isBound,
        bool isMismatch,
        string label,
        string detail) =>
        new(
            surface,
            dbRef,
            snapshot,
            tl,
            isBound,
            isMismatch,
            label,
            detail,
            isMismatch
                ? "editor-db-binding editor-db-binding--mismatch"
                : isBound ? "editor-db-binding editor-db-binding--bound" : "editor-db-binding editor-db-binding--unbound");

    private static string Label(string dbRef, string snapshot, string tl, bool mismatch)
    {
        var prefix = mismatch ? "DB MISMATCH" : "DB";
        return $"{prefix}: {Or(dbRef)} · snapshot {Or(snapshot)} · TL {Or(tl)}";
    }

    private static string Or(string value) => value.Length == 0 ? Unset : value;

    private static string Meta(PlatformWorkbook workbook, string key)
    {
        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        if (meta is null)
        {
            return string.Empty;
        }

        foreach (var row in meta.Rows)
        {
            if (row.Count >= 2 && string.Equals(row[0], key, StringComparison.Ordinal))
            {
                return row[1];
            }
        }

        return string.Empty;
    }
}
