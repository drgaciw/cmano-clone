namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Resolves selected-unit envelope ranges from catalog weapon rows and platform sensor fittings (meters → nm).
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
    /// Resolves max sensor envelope nm from platform combat radius and approved sensor bindings
    /// (combatRadiusNm × clamp(basePd, 0.05, 1.0); kill-chain envelope parity).
    /// Returns false when catalog is null, platform is unknown, combat radius is non-positive,
    /// or no approved sensor bindings exist for the platform.
    /// </summary>
    public static bool TryResolveSensorRangeNm(
        ICatalogReader? catalog,
        string? platformId,
        out double maxRangeNm)
    {
        maxRangeNm = 0;
        if (catalog is null || string.IsNullOrWhiteSpace(platformId))
        {
            return false;
        }

        if (!catalog.TryGetCombatRadiusNm(platformId, out var combatRadiusNm) || combatRadiusNm <= 0)
        {
            return false;
        }

        var any = false;
        foreach (var sensor in catalog.GetSortedSensorBindings())
        {
            if (!string.Equals(sensor.PlatformId, platformId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsApprovedSensorBinding(sensor))
            {
                continue;
            }

            var scale = Math.Clamp(sensor.BasePd, 0.05, 1.0);
            maxRangeNm = Math.Max(maxRangeNm, combatRadiusNm * scale);
            any = true;
        }

        return any && maxRangeNm > 0;
    }

    /// <summary>
    /// Resolves (sensorNm, weaponNm) for the selected unit.
    /// Sensor range comes from catalog platform sensor fittings when present; weapon range from
    /// catalog weapon envelope when present; both fall back to
    /// <see cref="DefaultSensorRangeNm"/> / <see cref="DefaultWeaponRangeNm"/>.
    /// </summary>
    public static (double SensorNm, double WeaponNm) ResolveSelectedUnitRanges(
        ICatalogReader? catalog,
        string? unitId,
        string weaponId = CatalogWeaponIds.MvpDefault)
    {
        var sensorNm = DefaultSensorRangeNm;
        var weaponNm = DefaultWeaponRangeNm;

        if (!string.IsNullOrWhiteSpace(unitId) &&
            TryResolveSensorRangeNm(catalog, unitId, out var sensorRangeNm))
        {
            sensorNm = sensorRangeNm;
        }

        if (TryResolveWeaponRangeNm(catalog, weaponId, out var weaponRangeNm))
        {
            weaponNm = weaponRangeNm;
        }

        return (sensorNm, weaponNm);
    }

    private static bool IsApprovedSensorBinding(CatalogSensorBinding sensor) =>
        string.Equals(sensor.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase);
}
