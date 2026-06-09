namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 Phase A: weapon stores loaded into a mount under a loadout
/// (sorted by platform_id, loadout_id, mount_id, weapon_id). Feeds doc 16 magazine→mount→weapon chain.
/// </summary>
public sealed record CatalogMagazineEntry(
    string PlatformId,
    string LoadoutId,
    string MountId,
    string WeaponId,
    int Quantity = 0,
    int ReloadTimeSec = 0,
    int Depth = 0);
