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
    public void ProjectPolyline_preserves_point_count()
    {
        var camera = GlobeViewProjection.DefaultBalticTheater().Camera;
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
}
