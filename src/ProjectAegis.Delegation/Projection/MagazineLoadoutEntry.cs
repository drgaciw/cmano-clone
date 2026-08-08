namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One magazine / mount / weapon row for CMD-24 loadout depth (presentation only).
/// </summary>
public sealed record MagazineLoadoutEntry(
    string UnitId,
    string PlatformId,
    string MountId,
    string WeaponId,
    string WeaponLabel,
    int Remaining,
    int Capacity,
    double FillPct,
    string StatusLine);
