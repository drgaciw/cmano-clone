namespace ProjectAegis.Delegation.Projection;

/// <summary>ADR-011 Phase C: case-insensitive PlatformId filter over browse rows (read-only).
/// S39-03 residual polish (S37/S38 carry): extended to match formatted display row too for better residual filters/density UX in Platform Editor (search on hp/res/speed etc values).
/// Strictly per polish-scope-boundary-2026-06-19.md + S37 precedent; extend-only CatalogWriteGate; no DelegationBridge.cs touch; C2 18/18+ / Graph* preserved; headless primary; lean evidence.
/// No new scope/features.
/// </summary>
public static class PlatformCatalogFilterProjection
{
    public static IReadOnlyList<CatalogPlatformBrowseRow> Apply(
        IReadOnlyList<CatalogPlatformBrowseRow> rows,
        string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return rows;
        }

        // S39-03: residual filter polish — ID + formatted list line (density/search UX carry from S36-15/S37-13)
        return rows
            .Where(r =>
                r.PlatformId.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                PlatformCatalogListProjection.FormatRow(r).Contains(filterText, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}