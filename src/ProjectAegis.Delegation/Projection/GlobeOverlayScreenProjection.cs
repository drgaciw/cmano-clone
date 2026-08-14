namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Normalized viewport point (0–1) for globe overlay screen bind. Values may fall outside 0–1 when off-screen.
/// </summary>
public readonly record struct GlobeViewportPoint(double X, double Y);

/// <summary>
/// Screen-space projection helpers for globe overlay bind (presentation-only).
/// Maps WGS84 points to normalized viewport coordinates from <see cref="GlobeCameraState"/>.
/// </summary>
public static class GlobeOverlayScreenProjection
{
    /// <summary>Minimum visible span in degrees to avoid divide-by-zero at poles.</summary>
    public const double MinVisibleSpanDegrees = 0.25;

    /// <summary>
    /// Approximate visible lat/lon span (degrees) from camera altitude — simple overview model.
    /// Higher altitude → wider span. Not a true Cesium frustum; sufficient for Toolkit overlay bind.
    /// </summary>
    public static (double LatSpanDegrees, double LonSpanDegrees) ResolveVisibleSpan(GlobeCameraState camera)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        // Altitude-driven span: ~2× ground footprint heuristic from camera height.
        var latSpan = Math.Max(camera.AltitudeMeters / MetersPerDegreeLatitude * 2.0, MinVisibleSpanDegrees);
        var cosLat = Math.Max(Math.Cos(camera.Latitude * Math.PI / 180.0), 0.05);
        var lonSpan = Math.Max(latSpan / cosLat, MinVisibleSpanDegrees);
        return (latSpan, lonSpan);
    }

    /// <summary>
    /// Project WGS84 to normalized viewport (0=center). Y increases north/up on screen.
    /// </summary>
    public static GlobeViewportPoint Project(
        double latitude,
        double longitude,
        GlobeCameraState camera)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        var (latSpan, lonSpan) = ResolveVisibleSpan(camera);
        var x = (longitude - camera.Longitude) / lonSpan + 0.5;
        var y = (latitude - camera.Latitude) / latSpan + 0.5;
        return new GlobeViewportPoint(x, y);
    }

    /// <summary>Project a ring polyline to viewport points (skips empty input).</summary>
    public static IReadOnlyList<GlobeViewportPoint> ProjectPolyline(
        IReadOnlyList<(double Latitude, double Longitude)> polyline,
        GlobeCameraState camera)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        if (polyline is null || polyline.Count == 0)
        {
            return Array.Empty<GlobeViewportPoint>();
        }

        var projected = new List<GlobeViewportPoint>(polyline.Count);
        foreach (var point in polyline)
        {
            projected.Add(Project(point.Latitude, point.Longitude, camera));
        }

        return projected;
    }

    private const double MetersPerDegreeLatitude = 111_320.0;
}
