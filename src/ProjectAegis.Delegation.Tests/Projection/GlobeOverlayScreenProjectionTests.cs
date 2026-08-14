using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeOverlayScreenProjectionTests
{
    [Test]
    public void Project_maps_camera_center_to_viewport_center()
    {
        var camera = new GlobeCameraState(60.0, 25.0, 1_000_000, 0, -45);

        var point = GlobeOverlayScreenProjection.Project(60.0, 25.0, camera);

        Assert.That(point.X, Is.EqualTo(0.5).Within(0.001));
        Assert.That(point.Y, Is.EqualTo(0.5).Within(0.001));
    }

    [Test]
    public void Project_heading_rotates_east_point_to_screen_right_when_facing_north()
    {
        var camera = new GlobeCameraState(60.0, 25.0, 1_000_000, HeadingDeg: 0, PitchDeg: -90);
        var (eastM, _) = GlobeOverlayScreenProjection.ToLocalTangentMeters(
            60.0,
            25.1,
            camera.Latitude,
            camera.Longitude);
        Assert.That(eastM, Is.GreaterThan(0));

        var point = GlobeOverlayScreenProjection.Project(60.0, 25.1, camera);

        Assert.That(point.X, Is.GreaterThan(0.5));
        Assert.That(point.Y, Is.EqualTo(0.5).Within(0.05));
    }

    [Test]
    public void TryProject_culls_points_behind_camera_when_oblique()
    {
        var camera = new GlobeCameraState(60.0, 25.0, 1_000_000, 0, -45);

        Assert.That(
            GlobeOverlayScreenProjection.TryProject(59.5, 25.0, camera, out _, cullOccluded: true),
            Is.False);
        Assert.That(
            GlobeOverlayScreenProjection.TryProject(60.5, 25.0, camera, out _, cullOccluded: true),
            Is.True);
    }

    [Test]
    public void ProjectPolyline_preserves_point_count_for_top_down_view()
    {
        var camera = new GlobeCameraState(60.0, 25.0, 1_000_000, 0, -90);
        var polyline = GlobeOverlayProjection.BuildRingPolyline(60.0, 25.0, rangeNm: 20, segments: 6);

        var projected = GlobeOverlayScreenProjection.ProjectPolyline(polyline, camera);

        Assert.That(projected, Has.Count.EqualTo(polyline.Count));
    }

    [Test]
    public void ResolveVisibleSpan_increases_with_altitude()
    {
        var low = new GlobeCameraState(60, 25, 500_000, 0, -45);
        var high = new GlobeCameraState(60, 25, 2_000_000, 0, -45);

        var lowSpan = GlobeOverlayScreenProjection.ResolveVisibleSpan(low);
        var highSpan = GlobeOverlayScreenProjection.ResolveVisibleSpan(high);

        Assert.That(highSpan.LatSpanDegrees, Is.GreaterThan(lowSpan.LatSpanDegrees));
        Assert.That(highSpan.LonSpanDegrees, Is.GreaterThan(lowSpan.LonSpanDegrees));
    }

    [Test]
    public void ResolvePitchForeshortening_top_down_is_one_horizon_near_zero()
    {
        Assert.That(GlobeOverlayScreenProjection.ResolvePitchForeshortening(-90), Is.EqualTo(1.0).Within(0.01));
        Assert.That(GlobeOverlayScreenProjection.ResolvePitchForeshortening(0), Is.LessThan(0.1));
    }
}
