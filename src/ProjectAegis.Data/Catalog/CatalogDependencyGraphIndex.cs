namespace ProjectAegis.Data.Catalog;

/// <summary>Builds sorted platform→mount→weapon and platform→sensor dependency edges (DBI-1.5).</summary>
public static class CatalogDependencyGraphIndex
{
    public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(ICatalogReader reader) =>
        BuildFrom(
            reader.GetSortedMounts(),
            reader.GetSortedMagazines(),
            reader.GetSortedSensorBindings());

    public static IReadOnlyList<CatalogDependencyEdge> BuildFrom(
        IReadOnlyList<CatalogMount> mounts,
        IReadOnlyList<CatalogMagazineEntry> magazines,
        IReadOnlyList<CatalogSensorBinding> sensors)
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

        return edges
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.MountId, StringComparer.Ordinal)
            .ThenBy(e => e.WeaponId, StringComparer.Ordinal)
            .ThenBy(e => e.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsApprovedBinding(CatalogMount mount) =>
        string.Equals(mount.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovedBinding(CatalogSensorBinding sensor) =>
        string.Equals(sensor.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);

    private static string MountKey(string platformId, string mountId) => $"{platformId}\0{mountId}";
}