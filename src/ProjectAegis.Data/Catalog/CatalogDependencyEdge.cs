namespace ProjectAegis.Data.Catalog;

/// <summary>DBI-1.5 kill-chain dependency edge with stable sort keys (ordinal).</summary>
public enum CatalogDependencyEdgeKind
{
    PlatformToMount,
    PlatformToMountToWeapon,
    PlatformToSensor,
}

/// <summary>
/// Materialized catalog dependency edge. Unused dimensions are empty strings;
/// <see cref="Kind"/> distinguishes platform→mount, platform→mount→weapon, and platform→sensor.
/// </summary>
public sealed record CatalogDependencyEdge(
    string PlatformId,
    string MountId = "",
    string WeaponId = "",
    string SensorId = "")
{
    public CatalogDependencyEdgeKind Kind =>
        !string.IsNullOrEmpty(SensorId)
            ? CatalogDependencyEdgeKind.PlatformToSensor
            : !string.IsNullOrEmpty(WeaponId)
                ? CatalogDependencyEdgeKind.PlatformToMountToWeapon
                : CatalogDependencyEdgeKind.PlatformToMount;
}