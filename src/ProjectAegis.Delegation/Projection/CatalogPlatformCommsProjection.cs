namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;

/// <summary>ADR-011 Phase G: read-only comms fittings for a single platform browse selection.</summary>
public static class CatalogPlatformCommsProjection
{
    public static IReadOnlyList<CatalogCommsBinding> ForPlatform(
        PlatformCatalogExportData data,
        string platformId) =>
        ForPlatform(data.Comms ?? [], platformId);

    public static IReadOnlyList<CatalogCommsBinding> ForPlatform(
        IReadOnlyList<CatalogCommsBinding> allComms,
        string platformId) =>
        allComms
            .Where(c => string.Equals(c.PlatformId, platformId, StringComparison.Ordinal))
            .OrderBy(c => c.LinkId, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<CatalogCommsBinding> ForPlatform(ICatalogReader reader, string platformId) =>
        ForPlatform(reader.GetSortedComms(), platformId);
}