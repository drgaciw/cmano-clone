namespace ProjectAegis.Delegation.Projection;

/// <summary>ADR-011 Phase C: case-insensitive PlatformId filter over browse rows (read-only).</summary>
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

        return rows
            .Where(r => r.PlatformId.Contains(filterText, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}