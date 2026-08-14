namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;

/// <summary>
/// Builds unit-pair datalink mesh edges from friendly OOB ids + catalog links (CMD-32 feed).
/// Adjacent pairs along sorted unit ids form a simple mesh for map network overlay.
/// </summary>
public static class DatalinkUnitPairFeed
{
    /// <summary>
    /// Creates adjacent pairs along sorted unit ids (u0→u1, u1→u2, …) keyed by a catalog link id.
    /// Uses <paramref name="preferredLinkId"/> when present in catalog; otherwise the first
    /// tactical catalog link; otherwise the first catalog link. Empty units or empty catalog
    /// links yield an empty mesh. Status is applied later by <see cref="ProjectEdges"/>.
    /// </summary>
    public static IReadOnlyList<(string FromUnitId, string ToUnitId, string LinkId)> BuildMesh(
        IReadOnlyList<string> friendlyUnitIds,
        IReadOnlyList<CatalogLinkEntry> catalogLinks,
        string? preferredLinkId = null)
    {
        if (friendlyUnitIds is null)
        {
            throw new ArgumentNullException(nameof(friendlyUnitIds));
        }

        if (catalogLinks is null)
        {
            throw new ArgumentNullException(nameof(catalogLinks));
        }

        if (friendlyUnitIds.Count == 0 || catalogLinks.Count == 0)
        {
            return Array.Empty<(string, string, string)>();
        }

        var linkId = ResolvePrimaryLinkId(catalogLinks, preferredLinkId);
        if (linkId is null)
        {
            return Array.Empty<(string, string, string)>();
        }

        var sorted = friendlyUnitIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        if (sorted.Length < 2)
        {
            return Array.Empty<(string, string, string)>();
        }

        var pairs = new List<(string, string, string)>(sorted.Length - 1);
        for (var i = 0; i < sorted.Length - 1; i++)
        {
            pairs.Add((sorted[i], sorted[i + 1], linkId));
        }

        return pairs;
    }

    /// <summary>
    /// Convenience: build mesh then project via <see cref="DatalinkPictureProjection.Project"/>.
    /// When <paramref name="commsSnapshot"/> is present, edge status reflects live comms projection
    /// (Nominal→Up, Degraded→Degraded, Denied→Down). Without snapshot data, status defaults to
    /// <see cref="DatalinkPictureProjection.StatusUp"/>. Empty mesh or catalog links yield empty edges.
    /// </summary>
    public static IReadOnlyList<DatalinkEdgeEntry> ProjectEdges(
        IReadOnlyList<string> friendlyUnitIds,
        IReadOnlyList<CatalogLinkEntry> catalogLinks,
        string? preferredLinkId = null,
        CommsStateSnapshot? commsSnapshot = null)
    {
        var mesh = BuildMesh(friendlyUnitIds, catalogLinks, preferredLinkId);
        if (mesh.Count == 0)
        {
            return Array.Empty<DatalinkEdgeEntry>();
        }

        return DatalinkPictureProjection.Project(
            mesh,
            catalogLinks,
            status: ResolveEdgeStatus(commsSnapshot));
    }

    /// <summary>
    /// Maps comms projection snapshot to datalink edge status for CMD-32 overlays.
    /// Null snapshot falls back to <see cref="DatalinkPictureProjection.StatusUp"/>.
    /// </summary>
    public static string ResolveEdgeStatus(CommsStateSnapshot? commsSnapshot) =>
        commsSnapshot is null
            ? DatalinkPictureProjection.StatusUp
            : MapCommsStateToEdgeStatus(commsSnapshot.State);

    private static string MapCommsStateToEdgeStatus(CommsState state) =>
        state switch
        {
            CommsState.Degraded => DatalinkPictureProjection.StatusDegraded,
            CommsState.Denied => DatalinkPictureProjection.StatusDown,
            _ => DatalinkPictureProjection.StatusUp,
        };

    private static string? ResolvePrimaryLinkId(
        IReadOnlyList<CatalogLinkEntry> catalogLinks,
        string? preferredLinkId)
    {
        if (!string.IsNullOrWhiteSpace(preferredLinkId))
        {
            foreach (var entry in catalogLinks)
            {
                if (entry is not null
                    && string.Equals(entry.LinkId, preferredLinkId, StringComparison.Ordinal))
                {
                    return entry.LinkId;
                }
            }
        }

        CatalogLinkEntry? firstAny = null;
        foreach (var entry in catalogLinks)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.LinkId))
            {
                continue;
            }

            firstAny ??= entry;
            if (string.Equals(entry.LinkType, CatalogLinkTypes.Tactical, StringComparison.Ordinal))
            {
                return entry.LinkId;
            }
        }

        return firstAny?.LinkId;
    }
}
