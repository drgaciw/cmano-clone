namespace ProjectAegis.Data.Catalog;

/// <summary>DBI-1.5 kill-chain dependency edge with stable sort keys (ordinal). Extended S36 for PlatformToLink (read-only comms FKs).</summary>
public enum CatalogDependencyEdgeKind
{
    PlatformToMount,
    PlatformToMountToWeapon,
    PlatformToSensor,
    PlatformToLink,
}

/// <summary>
/// Materialized catalog dependency edge. Unused dimensions are empty strings;
/// <see cref="Kind"/> distinguishes platform→mount, platform→mount→weapon, platform→sensor, and platform→link (S36+).
/// LinkId + CommsFittingId populated only for PlatformToLink (synthetic from platform_comms + link_catalog).
/// </summary>
public sealed record CatalogDependencyEdge(
    string PlatformId,
    string MountId = "",
    string WeaponId = "",
    string SensorId = "",
    string LinkId = "",
    string CommsFittingId = "")
{
    public CatalogDependencyEdgeKind Kind =>
        !string.IsNullOrEmpty(LinkId)
            ? CatalogDependencyEdgeKind.PlatformToLink
            : !string.IsNullOrEmpty(SensorId)
                ? CatalogDependencyEdgeKind.PlatformToSensor
                : !string.IsNullOrEmpty(WeaponId)
                    ? CatalogDependencyEdgeKind.PlatformToMountToWeapon
                    : CatalogDependencyEdgeKind.PlatformToMount;
}