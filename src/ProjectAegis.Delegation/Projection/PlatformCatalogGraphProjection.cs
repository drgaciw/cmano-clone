namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>PE-UX-W5: format dependency graph lines with focus / search beyond display cap.</summary>
public static class PlatformCatalogGraphProjection
{
    public const int DefaultDisplayCap = 20;

    public static IReadOnlyList<string> FormatLines(
        IReadOnlyList<CatalogDependencyEdge> edges,
        string? focusPlatformId = null,
        string? search = null,
        int displayCap = DefaultDisplayCap)
    {
        if (edges is null || edges.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(focusPlatformId))
            {
                return [$"(no graph edges for {focusPlatformId})"];
            }

            return Array.Empty<string>();
        }

        IEnumerable<CatalogDependencyEdge> query = edges;
        if (!string.IsNullOrWhiteSpace(focusPlatformId))
        {
            query = query.Where(e =>
                string.Equals(e.PlatformId, focusPlatformId, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            query = query.Where(e => FormatEdgeLine(e).Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = query.ToArray();
        if (filtered.Length == 0 && !string.IsNullOrWhiteSpace(focusPlatformId))
        {
            return [$"(no graph edges for {focusPlatformId})"];
        }

        // Focused platform: show all matching edges. Unfocused: apply display cap.
        IEnumerable<CatalogDependencyEdge> capped = !string.IsNullOrWhiteSpace(focusPlatformId)
            || filtered.Length <= displayCap
            ? filtered
            : filtered.Take(displayCap);

        return capped.Select(FormatEdgeLine).ToArray();
    }

    public static string FormatEdgeLine(CatalogDependencyEdge edge)
    {
        if (edge is null)
        {
            throw new ArgumentNullException(nameof(edge));
        }

        return edge.Kind switch
        {
            CatalogDependencyEdgeKind.PlatformToLink =>
                $"{edge.Kind}: {edge.PlatformId} → link={edge.LinkId} fitting={edge.CommsFittingId}",
            CatalogDependencyEdgeKind.PlatformToSensor =>
                $"{edge.Kind}: {edge.PlatformId} → sensor={edge.SensorId}",
            CatalogDependencyEdgeKind.PlatformToMountToWeapon =>
                $"{edge.Kind}: {edge.PlatformId} → mount={edge.MountId} weapon={edge.WeaponId}",
            _ =>
                $"{edge.Kind}: {edge.PlatformId} → mount={edge.MountId}",
        };
    }
}
