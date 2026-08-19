using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapPictureProjectionTests
{
    [Test]
    public void Place_is_stable_for_same_key_and_seed()
    {
        var a = MapPictureProjection.Place("u1", 42);
        var b = MapPictureProjection.Place("u1", 42);
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void Project_includes_friendly_and_hostile_symbols()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            42);
        Assert.That(symbols.Any(s => s.Affiliation == "Friendly"), Is.True);
        Assert.That(symbols.Any(s => s.Affiliation == "Hostile"), Is.True);
    }

    [Test]
    public void Project_uses_app6_distinct_glyphs_and_sidc_fields()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            42);

        var friendly = symbols.Single(s => s.Affiliation == "Friendly");
        var hostile = symbols.Single(s => s.Affiliation == "Hostile");

        Assert.That(friendly.ShapeGlyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.ShapeGlyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.ShapeGlyph, Is.Not.EqualTo(hostile.ShapeGlyph));
        Assert.That(App6Sidc.IsValidSidc(friendly.App6Sidc), Is.True);
        Assert.That(App6Sidc.IsValidSidc(hostile.App6Sidc), Is.True);
        Assert.That(friendly.App6UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.App6UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.App6UssFrameId, Is.Not.EqualTo(hostile.App6UssFrameId));
    }

    [Test]
    public void Project_preserves_deterministic_ordering()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u2", true), new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c2", "hostile-2", "u1", "Detected", 1, 1.0),
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            7);

        var ids = symbols.Select(s => s.SymbolId).ToArray();
        Assert.That(ids, Is.EqualTo(new[] { "u1", "u2", "c1", "c2" }));
    }

    [Test]
    public void Project_without_pose_keeps_hash_and_marks_unknown()
    {
        var hash = MapPictureProjection.Place("u1", 42);
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            Array.Empty<ContactPictureEntry>(),
            42);

        Assert.That(symbols[0].NormalizedX, Is.EqualTo(hash.X).Within(1e-6f));
        Assert.That(symbols[0].NormalizedY, Is.EqualTo(hash.Y).Within(1e-6f));
        Assert.That(symbols[0].HasAuthoritativePose, Is.False);
    }

    [Test]
    public void Project_uses_normalized_pose_instead_of_hash()
    {
        var hash = MapPictureProjection.Place("u1", 42);
        var poses = new Dictionary<string, UnitKinematicPose>(StringComparer.Ordinal)
        {
            ["u1"] = new(null, null, 0.31f, 0.44f, 90f, 18f),
        };

        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            Array.Empty<ContactPictureEntry>(),
            42,
            poses);

        Assert.That(symbols[0].HasAuthoritativePose, Is.True);
        Assert.That(symbols[0].NormalizedX, Is.EqualTo(0.31f).Within(1e-6f));
        Assert.That(symbols[0].NormalizedY, Is.EqualTo(0.44f).Within(1e-6f));
        Assert.That(symbols[0].NormalizedX, Is.Not.EqualTo(hash.X).Within(1e-4f));
        Assert.That(symbols[0].CourseDeg, Is.EqualTo(90f));
        Assert.That(symbols[0].SpeedNmPerHour, Is.EqualTo(18f));
    }

    [Test]
    public void Project_uses_lat_lon_pose_instead_of_hash()
    {
        var hash = MapPictureProjection.Place("u1", 7);
        var poses = new Dictionary<string, UnitKinematicPose>(StringComparer.Ordinal)
        {
            ["u1"] = new(60.0, 24.75, null, null, 0f, 12f),
        };

        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            Array.Empty<ContactPictureEntry>(),
            7,
            poses);

        var expected = MapPictureProjection.ProjectLatLon(60.0, 24.75);
        Assert.That(symbols[0].HasAuthoritativePose, Is.True);
        Assert.That(symbols[0].NormalizedX, Is.EqualTo(expected.X).Within(1e-5f));
        Assert.That(symbols[0].NormalizedY, Is.EqualTo(expected.Y).Within(1e-5f));
        Assert.That(symbols[0].Latitude, Is.EqualTo(60.0));
        Assert.That(symbols[0].Longitude, Is.EqualTo(24.75));
        Assert.That(Math.Abs(symbols[0].NormalizedX - hash.X), Is.GreaterThan(1e-4f));
    }

    [Test]
    public void ProjectCourses_emits_polyline_from_pose_through_waypoints()
    {
        var poses = new Dictionary<string, UnitKinematicPose>(StringComparer.Ordinal)
        {
            ["u1"] = new(null, null, 0.2f, 0.2f, 45f, 22f),
        };
        var courses = new Dictionary<string, IReadOnlyList<CourseWaypoint>>(StringComparer.Ordinal)
        {
            ["u1"] = [new CourseWaypoint(0.4f, 0.3f), new CourseWaypoint(0.6f, 0.5f)],
        };

        var overlays = MapPictureProjection.ProjectCourses(
            [new OobTreeEntry("u1", true)],
            courses,
            poses,
            42);

        Assert.That(overlays, Has.Count.EqualTo(1));
        Assert.That(overlays[0].Vertices, Has.Count.EqualTo(3));
        Assert.That(overlays[0].Vertices[0].NormalizedX, Is.EqualTo(0.2f).Within(1e-6f));
        Assert.That(overlays[0].Vertices[2].NormalizedX, Is.EqualTo(0.6f).Within(1e-6f));
    }

    [Test]
    public void ProjectCourses_skips_destroyed_units()
    {
        var courses = new Dictionary<string, IReadOnlyList<CourseWaypoint>>(StringComparer.Ordinal)
        {
            ["u1"] = [new CourseWaypoint(0.2f, 0.2f), new CourseWaypoint(0.4f, 0.4f)],
        };

        var overlays = MapPictureProjection.ProjectCourses(
            [new OobTreeEntry("u1", false)],
            courses,
            poses: null,
            layoutSeed: 1);

        Assert.That(overlays, Is.Empty);
    }
}