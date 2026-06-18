namespace ProjectAegis.Data.Catalog;

/// <summary>Req-06 entity-to-table map for SQLite catalog (P0 scope).</summary>
public static class CatalogEntityMap
{
    public sealed record EntityTableBinding(
        string EntityName,
        string TableName,
        string PrimaryKeyColumns,
        string DeterministicOrderBy,
        string RuntimeDto);

    public static IReadOnlyList<EntityTableBinding> All { get; } =
    [
        new("CatalogSensorBinding", "sensor", "platform_id,sensor_id",
            "platform_id ASC, sensor_id ASC", nameof(CatalogSensorBinding)),
        new("CatalogPlatformBinding", "platform", "platform_id,snapshot_id",
            "platform_id ASC", nameof(CatalogPlatformBinding)),
        new("CatalogWeaponRecord", "weapon_catalog", "weapon_id",
            "weapon_id ASC", nameof(CatalogWeaponRecord)),
        new("CatalogMount", "platform_mount", "platform_id,mount_id",
            "platform_id ASC, mount_id ASC", nameof(CatalogMount)),
        new("CatalogLoadout", "platform_loadout", "platform_id,loadout_id",
            "platform_id ASC, loadout_id ASC", nameof(CatalogLoadout)),
        new("CatalogMagazineEntry", "platform_magazine", "platform_id,loadout_id,mount_id,weapon_id",
            "platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC", nameof(CatalogMagazineEntry)),
        new("CatalogCommsBinding", "platform_comms", "platform_id,link_id",
            "platform_id ASC, link_id ASC", nameof(CatalogCommsBinding)),
        new("QuarantinedCatalogBinding", "sensor_quarantine", "platform_id,sensor_id",
            "platform_id ASC, sensor_id ASC", nameof(QuarantinedCatalogBinding)),
        new("CatalogPlatformEntry", "platform", "platform_id,snapshot_id",
            "platform_id ASC, snapshot_id ASC", nameof(CatalogPlatformEntry)),
        new("CatalogSnapshot", "catalog_snapshot", "snapshot_id",
            "snapshot_id ASC", "snapshot_id"),
        new("CatalogStagingSensor", "catalog_staging_sensor", "batch_id,platform_id,sensor_id",
            "batch_id ASC, platform_id ASC, sensor_id ASC", "CatalogStagingSensorRow"),
        new("CatalogChangeLogEntry", "catalog_change_log", "change_id",
            "batch_id ASC, change_id ASC", nameof(CatalogChangeLogEntry)),
        new("DbRelease", "db_release", "release_version",
            "release_version ASC", nameof(DbReleaseRecord)),
    ];

    public static bool TryGetTable(string entityName, out EntityTableBinding binding)
    {
        foreach (var row in All)
        {
            if (string.Equals(row.EntityName, entityName, StringComparison.Ordinal))
            {
                binding = row;
                return true;
            }
        }

        binding = null!;
        return false;
    }
}