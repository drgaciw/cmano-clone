namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Builds sorted platform→mount→weapon and platform→sensor dependency edges (DBI-1.5).
/// S36+ extends with PlatformToLink (read-only, approved comms FKs to link_catalog).
/// S37-03: full kill-chain surfacing (platform→link + weapon→mount→sensor chains + API).
/// Deterministic Ordinal sort only; extend-only path; hash-invariant for callers.
/// </summary>
public static class CatalogDependencyGraphIndex
{
    /// <summary>
    /// S37-03: explicit full kill-chain surfacing API.
    /// Returns complete set of platform→link + weapon→mount→sensor chains (all approved edge kinds).
    /// Delegates to BuildFrom (stable, deterministic).
    /// </summary>
    public static IReadOnlyList<CatalogDependencyEdge> BuildFullKillChain(ICatalogReader reader) =>
        BuildFrom(reader);

    /// <summary>
    /// S37-03: full kill-chain surfacing overload for direct inputs (platform→link + weapon→mount→sensor chains).
    /// </summary>
    public static IReadOnlyList<CatalogDependencyEdge> BuildFullKillChain(
        IReadOnlyList<CatalogMount> mounts,
        IReadOnlyList<CatalogMagazineEntry> magazines,
        IReadOnlyList<CatalogSensorBinding> sensors,
        IReadOnlyList<CatalogCommsBinding> comms,
        IReadOnlyList<CatalogLinkEntry> links) =>
        BuildFrom(mounts, magazines, sensors, comms, links);
    public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(ICatalogReader reader) =>
        BuildFrom(
            reader.GetSortedMounts(),
            reader.GetSortedMagazines(),
            reader.GetSortedSensorBindings(),
            reader.GetSortedComms(),
            reader.GetSortedLinks());

    /// <summary>
    /// 3-arg overload for backward compat in existing tests/fixtures (no link edges emitted).
    /// Delegates to 5-arg with empty comms/links.
    /// </summary>
    public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(
        IReadOnlyList<CatalogMount> mounts,
        IReadOnlyList<CatalogMagazineEntry> magazines,
        IReadOnlyList<CatalogSensorBinding> sensors) =>
        BuildFrom(mounts, magazines, sensors, [], []);

    public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(
        IReadOnlyList<CatalogMount> mounts,
        IReadOnlyList<CatalogMagazineEntry> magazines,
        IReadOnlyList<CatalogSensorBinding> sensors,
        IReadOnlyList<CatalogCommsBinding> comms,
        IReadOnlyList<CatalogLinkEntry> links)
    {
        var edges = new List<CatalogDependencyEdge>();
        var approvedMounts = mounts
            .Where(IsApprovedBinding)
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ToArray();

        var approvedMountKeys = new HashSet<string>(
            approvedMounts.Select(m => MountKey(m.PlatformId, m.MountId)),
            StringComparer.Ordinal);

        foreach (var mount in approvedMounts)
        {
            edges.Add(new CatalogDependencyEdge(mount.PlatformId, mount.MountId));
        }

        foreach (var magazine in magazines
                     .Where(m => approvedMountKeys.Contains(MountKey(m.PlatformId, m.MountId)))
                     .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                     .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
                     .ThenBy(m => m.MountId, StringComparer.Ordinal)
                     .ThenBy(m => m.WeaponId, StringComparer.Ordinal))
        {
            edges.Add(new CatalogDependencyEdge(
                magazine.PlatformId,
                magazine.MountId,
                magazine.WeaponId));
        }

        foreach (var sensor in sensors.Where(IsApprovedBinding))
        {
            edges.Add(new CatalogDependencyEdge(sensor.PlatformId, SensorId: sensor.SensorId));
        }

        // S36: PlatformToLink edges — only approved comms with resolved link target (no orphan)
        var knownLinks = new HashSet<string>(links.Select(l => l.LinkId), StringComparer.Ordinal);
        foreach (var c in comms.Where(IsApprovedBinding))
        {
            if (knownLinks.Contains(c.LinkId))
            {
                edges.Add(new CatalogDependencyEdge(
                    c.PlatformId,
                    LinkId: c.LinkId,
                    CommsFittingId: CatalogSortKeyComparer.FormatCommsKey(c)));
            }
        }

        return edges
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.MountId, StringComparer.Ordinal)
            .ThenBy(e => e.WeaponId, StringComparer.Ordinal)
            .ThenBy(e => e.SensorId, StringComparer.Ordinal)
            .ThenBy(e => e.LinkId, StringComparer.Ordinal)
            .ThenBy(e => e.CommsFittingId, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsApprovedBinding(CatalogMount mount) =>
        string.Equals(mount.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedBinding(CatalogSensorBinding sensor) =>
        string.Equals(sensor.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedBinding(CatalogCommsBinding comms) =>
        string.Equals(comms.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static string MountKey(string platformId, string mountId) => $"{platformId}\0{mountId}";
}