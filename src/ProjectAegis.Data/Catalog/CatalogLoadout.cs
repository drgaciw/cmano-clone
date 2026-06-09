namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase A: a named loadout preset for a platform (sorted by platform_id, loadout_id).</summary>
public sealed record CatalogLoadout(
    string PlatformId,
    string LoadoutId,
    string LoadoutName = "",
    string Role = "",
    bool IsDefault = false);
