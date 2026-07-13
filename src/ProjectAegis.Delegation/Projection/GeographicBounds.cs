namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Inclusive WGS84 axis-aligned bounding box for a theater or camera jump target
/// (req 20 §Map and Symbology, TR-c2-004, ADR-018). Pure value type — no UnityEngine.
/// </summary>
public readonly record struct GeographicBounds(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude)
{
    /// <summary>Latitude span in degrees (always non-negative when constructed via <see cref="FromMinMax"/>).</summary>
    public double LatitudeSpan => MaxLatitude - MinLatitude;

    /// <summary>Longitude span in degrees (always non-negative when constructed via <see cref="FromMinMax"/>).</summary>
    public double LongitudeSpan => MaxLongitude - MinLongitude;

    /// <summary>Geographic center latitude (mean of min/max).</summary>
    public double CenterLatitude => (MinLatitude + MaxLatitude) * 0.5;

    /// <summary>Geographic center longitude (mean of min/max).</summary>
    public double CenterLongitude => (MinLongitude + MaxLongitude) * 0.5;

    /// <summary>True when latitude/longitude spans are both finite and strictly positive.</summary>
    public bool IsValid =>
        double.IsFinite(MinLatitude) &&
        double.IsFinite(MaxLatitude) &&
        double.IsFinite(MinLongitude) &&
        double.IsFinite(MaxLongitude) &&
        LatitudeSpan > 0.0 &&
        LongitudeSpan > 0.0;

    /// <summary>True if the point lies inside the box (inclusive edges).</summary>
    public bool Contains(double latitude, double longitude) =>
        latitude >= MinLatitude &&
        latitude <= MaxLatitude &&
        longitude >= MinLongitude &&
        longitude <= MaxLongitude;

    /// <summary>
    /// Build a bounds from two arbitrary corners (e.g. drag-box geo corners), normalizing min/max.
    /// </summary>
    public static GeographicBounds FromMinMax(
        double latitude0,
        double longitude0,
        double latitude1,
        double longitude1) =>
        new(
            Math.Min(latitude0, latitude1),
            Math.Min(longitude0, longitude1),
            Math.Max(latitude0, latitude1),
            Math.Max(longitude0, longitude1));
}
