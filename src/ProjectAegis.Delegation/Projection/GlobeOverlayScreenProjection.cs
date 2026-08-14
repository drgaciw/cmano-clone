namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Normalized viewport point (0–1) for globe overlay screen bind. Values may fall outside 0–1 when off-screen.
/// </summary>
public readonly record struct GlobeViewportPoint(double X, double Y);

/// <summary>
/// Screen-space projection helpers for globe overlay bind (presentation-only).
/// Maps WGS84 points to normalized viewport coordinates from <see cref="GlobeCameraState"/>
/// with heading rotation, pitch foreshortening, and simple back-hemisphere cull.
/// </summary>
public static class GlobeOverlayScreenProjection
{
    /// <summary>Minimum visible span in degrees to avoid divide-by-zero at poles.</summary>
    public const double MinVisibleSpanDegrees = 0.25;

    private const double MetersPerDegreeLatitude = 111_320.0;

    /// <summary>
    /// Approximate visible ground span (meters) from camera altitude and pitch.
    /// Pitch widens the east span when oblique; not a true Cesium frustum.
    /// </summary>
    public static (double EastSpanMeters, double NorthSpanMeters) ResolveVisibleSpanMeters(GlobeCameraState camera)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        var northSpan = Math.Max(camera.AltitudeMeters * 2.0, MinVisibleSpanDegrees * MetersPerDegreeLatitude);
        var pitchScale = ResolvePitchForeshortening(camera.PitchDeg);
        var eastSpan = northSpan / Math.Max(pitchScale, 0.15);
        return (eastSpan, northSpan);
    }

    /// <summary>
    /// Legacy degree-span helper retained for tests; derived from <see cref="ResolveVisibleSpanMeters"/>.
    /// </summary>
    public static (double LatSpanDegrees, double LonSpanDegrees) ResolveVisibleSpan(GlobeCameraState camera)
    {
        var (eastSpan, northSpan) = ResolveVisibleSpanMeters(camera);
        var cosLat = Math.Max(Math.Cos(camera.Latitude * Math.PI / 180.0), 0.05);
        return (
            northSpan / MetersPerDegreeLatitude,
            eastSpan / (MetersPerDegreeLatitude * cosLat));
    }

    /// <summary>
    /// Project WGS84 to normalized viewport (0.5=center). Y increases north/up on screen.
    /// Points behind the camera return false when <paramref name="cullOccluded"/> is true.
    /// </summary>
    public static bool TryProject(
        double latitude,
        double longitude,
        GlobeCameraState camera,
        out GlobeViewportPoint point,
        bool cullOccluded = true)
    {
        if (camera is null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        var (viewEast, viewNorth) = ToCameraViewMeters(latitude, longitude, camera);
        if (cullOccluded && IsBehindCamera(viewNorth, camera.PitchDeg))
        {
            point = default;
            return false;
        }

        var foreshortenedNorth = viewNorth * ResolvePitchForeshortening(camera.PitchDeg);
        var (spanEast, spanNorth) = ResolveVisibleSpanMeters(camera);
        point = new GlobeViewportPoint(
            viewEast / spanEast + 0.5,
            foreshortenedNorth / spanNorth + 0.5);
        return true;
    }

    /// <summary>
    /// Project WGS84 to normalized viewport without occlusion cull (always returns a point).
    /// </summary>
    public static GlobeViewportPoint Project(
        double latitude,
        double longitude,
        GlobeCameraState camera)
    {
        TryProject(latitude, longitude, camera, out var point, cullOccluded: false);
        return point;
    }

    /// <summary>Project a ring polyline to viewport points, breaking at occluded vertices.</summary>
    public static IReadOnlyList<GlobeViewportPoint> ProjectPolyline(
        IReadOnlyList<(double Latitude, double Longitude)> polyline,
        GlobeCameraState camera,
        bool cullOccluded = true)
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
        foreach (var vertex in polyline)
        {
            if (TryProject(vertex.Latitude, vertex.Longitude, camera, out var point, cullOccluded))
            {
                projected.Add(point);
            }
        }

        return projected;
    }

    /// <summary>Local east/north offsets in meters from camera center (tangent plane).</summary>
    public static (double EastMeters, double NorthMeters) ToLocalTangentMeters(
        double latitude,
        double longitude,
        double centerLatitude,
        double centerLongitude)
    {
        var cosLat = Math.Max(Math.Cos(centerLatitude * Math.PI / 180.0), 0.05);
        var eastM = (longitude - centerLongitude) * cosLat * MetersPerDegreeLatitude;
        var northM = (latitude - centerLatitude) * MetersPerDegreeLatitude;
        return (eastM, northM);
    }

    /// <summary>Rotate tangent offsets into camera view axes (heading clockwise from north).</summary>
    public static (double ViewEastMeters, double ViewNorthMeters) ToCameraViewMeters(
        double latitude,
        double longitude,
        GlobeCameraState camera)
    {
        var (eastM, northM) = ToLocalTangentMeters(
            latitude,
            longitude,
            camera.Latitude,
            camera.Longitude);
        var headingRad = camera.HeadingDeg * Math.PI / 180.0;
        var cos = Math.Cos(headingRad);
        var sin = Math.Sin(headingRad);
        var viewEast = eastM * cos - northM * sin;
        var viewNorth = eastM * sin + northM * cos;
        return (viewEast, viewNorth);
    }

    /// <summary>Pitch foreshortening: -90° top-down → 1.0; 0° horizon → ~0.</summary>
    public static double ResolvePitchForeshortening(double pitchDeg)
    {
        var obliquityDeg = 90.0 + pitchDeg;
        var radians = obliquityDeg * Math.PI / 180.0;
        return Math.Max(Math.Cos(radians), 0.05);
    }

    private static bool IsBehindCamera(double viewNorthMeters, double pitchDeg)
    {
        // Top-down and near-top-down views show the full local hemisphere.
        if (pitchDeg <= -85.0)
        {
            return false;
        }

        return viewNorthMeters < 0.0;
    }
}
