namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>ADR-011 Phase H: read-only global link catalog rows for platform catalog viewer.</summary>
public static class CatalogLinkListProjection
{
    public static IReadOnlyList<CatalogLinkEntry> FromReader(ICatalogReader reader) =>
        reader.GetSortedLinks();

    public static IReadOnlyDictionary<string, string> BuildDisplayNameLookup(
        IReadOnlyList<CatalogLinkEntry> links) =>
        links
            .Where(link => !string.IsNullOrWhiteSpace(link.DisplayName))
            .ToDictionary(link => link.LinkId, link => link.DisplayName, StringComparer.Ordinal);
}