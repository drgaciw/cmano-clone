namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Resolves selected-unit envelope ranges from catalog weapon rows (meters → nm).
/// Pure helpers for map overlay hosts (CMD-21/34 / Wave 2).
/// </summary>
public static class CatalogEnvelopeRangeResolver
{
    /// <summary>International nautical mile in meters.</summary>
    public const double MetersPerNauticalMile = 1852.0;

    /// <summary>Fallback sensor envelope nm when catalog/engage ranges are unavailable.</summary>
    public const double DefaultSensorRangeNm = 40.0;

    /// <summary>Fallback weapon envelope nm when catalog/engage ranges are unavailable.</summary>
    public const double DefaultWeaponRangeNm = 20.0;

    /// <summary>Converts meters to nautical miles (1 nm = 1852 m).</summary>
    public static double MetersToNauticalMiles(double meters) =>
        meters / MetersPerNauticalMile;

    /// <summary>
    /// Resolves max weapon range in nm from <see cref="ICatalogReader.TryGetWeaponEnvelope"/>.
    /// Returns false when catalog is null, weapon is unknown, or max range is non-positive.
    /// </summary>
    public static bool TryResolveWeaponRangeNm(
        ICatalogReader? catalog,
        string weaponId,
        out double maxRangeNm)
    {
        maxRangeNm = 0;
        if (catalog is null || string.IsNullOrWhiteSpace(weaponId))
        {
            return false;
        }

        if (!catalog.TryGetWeaponEnvelope(weaponId, out var envelope))
        {
            return false;
        }

        if (envelope.MaxRangeMeters <= 0)
        {
            return false;
        }

        maxRangeNm = MetersToNauticalMiles(envelope.MaxRangeMeters);
        return true;
    }

    /// <summary>
    /// Resolves (sensorNm, weaponNm) for the selected unit.
    /// Weapon range comes from catalog when present; both fall back to
    /// <see cref="DefaultSensorRangeNm"/> / <see cref="DefaultWeaponRangeNm"/>.
    /// <paramref name="unitId"/> is reserved for future per-unit sensor wiring and does not
    /// affect the current defaults when catalog weapon resolution succeeds or fails.
    /// </summary>
    public static (double SensorNm, double WeaponNm) ResolveSelectedUnitRanges(
        ICatalogReader? catalog,
        string? unitId,
        string weaponId = CatalogWeaponIds.MvpDefault)
    {
        _ = unitId; // reserved for per-unit sensor/platform reach wiring

        var sensorNm = DefaultSensorRangeNm;
        var weaponNm = DefaultWeaponRangeNm;

        if (TryResolveWeaponRangeNm(catalog, weaponId, out var maxRangeNm))
        {
            weaponNm = maxRangeNm;
        }

        return (sensorNm, weaponNm);
    }
}
