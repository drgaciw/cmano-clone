namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Pure datalink/comms network edge projection for the map picture (CMD-32).
/// Projects from explicit unit-link tuples, optionally resolving link type from catalog rows.
/// </summary>
public static class DatalinkPictureProjection
{
    public const string StatusUp = "Up";
    public const string StatusDegraded = "Degraded";
    public const string StatusDown = "Down";

    /// <summary>
    /// Projects edges from simple link tuples: (fromUnitId, toUnitId, linkType, status).
    /// Empty status defaults to <see cref="StatusUp"/>. Deterministic order by from, to, type.
    /// </summary>
    public static IReadOnlyList<DatalinkEdgeEntry> Project(
        IReadOnlyList<(string FromUnitId, string ToUnitId, string LinkType, string Status)> links)
    {
        if (links is null)
        {
            throw new ArgumentNullException(nameof(links));
        }

        if (links.Count == 0)
        {
            return Array.Empty<DatalinkEdgeEntry>();
        }

        var edges = new List<DatalinkEdgeEntry>(links.Count);
        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.FromUnitId) || string.IsNullOrWhiteSpace(link.ToUnitId))
            {
                continue;
            }

            var linkType = string.IsNullOrWhiteSpace(link.LinkType) ? string.Empty : link.LinkType;
            var status = NormalizeStatus(link.Status);
            edges.Add(new DatalinkEdgeEntry(link.FromUnitId, link.ToUnitId, linkType, status));
        }

        return SortEdges(edges);
    }

    /// <summary>
    /// Projects edges from unit pairs keyed by catalog link id, resolving
    /// <see cref="DatalinkEdgeEntry.LinkType"/> from <paramref name="catalogLinks"/> when present.
    /// Missing catalog rows fall back to the raw LinkId as LinkType.
    /// </summary>
    public static IReadOnlyList<DatalinkEdgeEntry> Project(
        IReadOnlyList<(string FromUnitId, string ToUnitId, string LinkId)> unitLinks,
        IReadOnlyList<CatalogLinkEntry> catalogLinks,
        string status = StatusUp)
    {
        if (unitLinks is null)
        {
            throw new ArgumentNullException(nameof(unitLinks));
        }

        if (catalogLinks is null)
        {
            throw new ArgumentNullException(nameof(catalogLinks));
        }

        if (unitLinks.Count == 0)
        {
            return Array.Empty<DatalinkEdgeEntry>();
        }

        var typeById = new Dictionary<string, string>(catalogLinks.Count, StringComparer.Ordinal);
        foreach (var entry in catalogLinks)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.LinkId))
            {
                continue;
            }

            typeById[entry.LinkId] = entry.LinkType ?? string.Empty;
        }

        var normalizedStatus = NormalizeStatus(status);
        var edges = new List<DatalinkEdgeEntry>(unitLinks.Count);
        foreach (var link in unitLinks)
        {
            if (string.IsNullOrWhiteSpace(link.FromUnitId) || string.IsNullOrWhiteSpace(link.ToUnitId))
            {
                continue;
            }

            var linkId = link.LinkId ?? string.Empty;
            var linkType = typeById.TryGetValue(linkId, out var resolved) ? resolved : linkId;
            edges.Add(new DatalinkEdgeEntry(link.FromUnitId, link.ToUnitId, linkType, normalizedStatus));
        }

        return SortEdges(edges);
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return StatusUp;
        }

        var trimmed = status.Trim();
        return trimmed switch
        {
            StatusDegraded => StatusDegraded,
            StatusDown => StatusDown,
            StatusUp => StatusUp,
            _ => trimmed,
        };
    }

    private static IReadOnlyList<DatalinkEdgeEntry> SortEdges(List<DatalinkEdgeEntry> edges) =>
        edges
            .OrderBy(e => e.FromUnitId, StringComparer.Ordinal)
            .ThenBy(e => e.ToUnitId, StringComparer.Ordinal)
            .ThenBy(e => e.LinkType, StringComparer.Ordinal)
            .ThenBy(e => e.Status, StringComparer.Ordinal)
            .ToArray();
}
