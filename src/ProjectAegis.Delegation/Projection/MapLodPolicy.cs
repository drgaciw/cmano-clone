namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure altitude → LOD band thresholds for map symbol clustering (REQ-20 Phase N).
/// Thresholds are camera altitude in meters above ellipsoid; higher altitude → coarser band.
/// </summary>
public static class MapLodPolicy
{
    /// <summary>Below this altitude → <see cref="MapLodBand.Close"/> (meters).</summary>
    public const double CloseMaxAltitudeMeters = 50_000.0;

    /// <summary>Below this altitude → <see cref="MapLodBand.Tactical"/> (meters).</summary>
    public const double TacticalMaxAltitudeMeters = 250_000.0;

    /// <summary>Below this altitude → <see cref="MapLodBand.Theater"/>; at/above → <see cref="MapLodBand.Overview"/> (meters).</summary>
    public const double TheaterMaxAltitudeMeters = 1_000_000.0;

    /// <summary>
    /// Resolve LOD band from camera altitude in meters.
    /// Negative or zero altitude maps to Close. Pure thresholds — no Unity.
    /// </summary>
    public static MapLodBand ResolveBand(double altitudeMeters)
    {
        if (altitudeMeters < CloseMaxAltitudeMeters)
        {
            return MapLodBand.Close;
        }

        if (altitudeMeters < TacticalMaxAltitudeMeters)
        {
            return MapLodBand.Tactical;
        }

        if (altitudeMeters < TheaterMaxAltitudeMeters)
        {
            return MapLodBand.Theater;
        }

        return MapLodBand.Overview;
    }

    /// <summary>
    /// Suggested default grid divisions for a band (callers may override).
    /// Close ignores grid; Overview uses coarsest grid (fewest cells → most reduction).
    /// </summary>
    public static int DefaultGridDivisions(MapLodBand band) =>
        band switch
        {
            MapLodBand.Overview => 16,
            MapLodBand.Theater => 24,
            MapLodBand.Tactical => 48,
            MapLodBand.Close => 1,
            _ => 16,
        };
}
