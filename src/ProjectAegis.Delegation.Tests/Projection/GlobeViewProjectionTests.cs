using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeViewProjectionTests
{
    [Test]
    public void DefaultBalticTheater_camera_over_baltic_bbox_with_3d_mode()
    {
        var state = GlobeViewProjection.DefaultBalticTheater();

        Assert.That(state.Camera.Latitude, Is.EqualTo(60.0).Within(1e-6));
        Assert.That(state.Camera.Longitude, Is.EqualTo(24.8).Within(1e-6));
        Assert.That(state.Camera.AltitudeMeters, Is.GreaterThan(100_000.0));
        Assert.That(state.Mode2d3d, Is.EqualTo(GlobeViewMode2d3d.ThreeD));
        Assert.That(state.ActiveBookmarkId, Is.EqualTo(GlobeViewProjection.BalticBookmarkId));
        Assert.That(GlobeViewProjection.ResolveTheaterLabel(state), Is.EqualTo("Baltic"));
    }

    [Test]
    public void Theater_presets_include_Baltic_GIUK_Pacific()
    {
        var presets = GlobeViewProjection.TheaterPresets;

        Assert.That(presets, Has.Count.EqualTo(3));
        Assert.That(presets[0].Id, Is.EqualTo(GlobeViewProjection.BalticBookmarkId));
        Assert.That(presets[0].Label, Is.EqualTo("Baltic"));
        Assert.That(presets[0].IsEmpty, Is.False);
        Assert.That(presets[1].Id, Is.EqualTo(GlobeViewProjection.GiukBookmarkId));
        Assert.That(presets[1].Label, Is.EqualTo("GIUK"));
        Assert.That(presets[1].Camera.Latitude, Is.EqualTo(65.0).Within(1e-6));
        Assert.That(presets[1].Camera.Longitude, Is.EqualTo(-20.0).Within(1e-6));
        Assert.That(presets[2].Id, Is.EqualTo(GlobeViewProjection.PacificBookmarkId));
        Assert.That(presets[2].Label, Is.EqualTo("Pacific"));
        Assert.That(presets[2].Camera.Latitude, Is.EqualTo(20.0).Within(1e-6));
        Assert.That(presets[2].Camera.Longitude, Is.EqualTo(140.0).Within(1e-6));
    }

    [Test]
    public void WithQuickJump_moves_camera_to_bookmark_without_mutating_input()
    {
        var original = GlobeViewProjection.DefaultBalticTheater();
        var jumped = GlobeViewProjection.WithQuickJump(original, GlobeViewProjection.GiukBookmarkId);

        Assert.That(jumped.ActiveBookmarkId, Is.EqualTo(GlobeViewProjection.GiukBookmarkId));
        Assert.That(jumped.Camera.Latitude, Is.EqualTo(65.0).Within(1e-6));
        Assert.That(jumped.Camera.Longitude, Is.EqualTo(-20.0).Within(1e-6));
        // Input state is a record; original camera remains Baltic.
        Assert.That(original.Camera.Latitude, Is.EqualTo(60.0).Within(1e-6));
        Assert.That(original.ActiveBookmarkId, Is.EqualTo(GlobeViewProjection.BalticBookmarkId));
        Assert.That(GlobeViewProjection.ResolveTheaterLabel(jumped), Is.EqualTo("GIUK"));
    }

    [Test]
    public void WithQuickJump_unknown_id_returns_same_state()
    {
        var original = GlobeViewProjection.DefaultBalticTheater();
        var jumped = GlobeViewProjection.WithQuickJump(original, "no-such-theater");

        Assert.That(jumped, Is.EqualTo(original));
    }

    [Test]
    public void EmptyBookmarksPresentation_is_honest_empty_not_disabled()
    {
        var empty = GlobeViewProjection.EmptyBookmarksPresentation();

        Assert.That(empty.IsEmpty, Is.True);
        Assert.That(empty.IsDisabled, Is.False);
        Assert.That(empty.Bookmarks, Is.Empty);
        Assert.That(empty.EmptyStateLine, Is.EqualTo(GlobeViewProjection.EmptyBookmarksLine));
        Assert.That(empty.EmptyStateLine, Does.Contain("No saved views"));
    }

    [Test]
    public void PresentBookmarks_all_empty_slots_uses_honest_empty()
    {
        var slots = new[]
        {
            GlobeTheaterBookmark.EmptySlot("1"),
            GlobeTheaterBookmark.EmptySlot("2"),
        };

        var presented = GlobeViewProjection.PresentBookmarks(slots);

        Assert.That(presented.IsEmpty, Is.True);
        Assert.That(presented.IsDisabled, Is.False);
        Assert.That(presented.EmptyStateLine, Is.EqualTo(GlobeViewProjection.EmptyBookmarksLine));
    }

    [Test]
    public void PresentBookmarks_live_presets_not_empty()
    {
        var presented = GlobeViewProjection.PresentBookmarks(GlobeViewProjection.TheaterPresets);

        Assert.That(presented.IsEmpty, Is.False);
        Assert.That(presented.IsDisabled, Is.False);
        Assert.That(presented.EmptyStateLine, Is.Empty);
        Assert.That(presented.Bookmarks, Has.Count.EqualTo(3));
    }

    [Test]
    public void WithMode_two_d_sets_top_down_pitch()
    {
        var threeD = GlobeViewProjection.DefaultBalticTheater();
        var twoD = GlobeViewProjection.WithMode(threeD, GlobeViewMode2d3d.TwoD);

        Assert.That(twoD.Mode2d3d, Is.EqualTo(GlobeViewMode2d3d.TwoD));
        Assert.That(twoD.Camera.PitchDeg, Is.EqualTo(-90.0).Within(1e-6));
        Assert.That(twoD.Camera.Latitude, Is.EqualTo(threeD.Camera.Latitude).Within(1e-6));
    }

    [Test]
    public void Project_uses_explicit_wgs84_when_symbol_has_lat_lon()
    {
        var symbols = new[]
        {
            new MapSymbolEntry(
                "carrier-1",
                "Friendly",
                "F",
                "Carrier",
                NormalizedX: 0f,
                NormalizedY: 0f,
                IsDestroyed: false,
                App6Sidc: null,
                App6UssFrameId: null,
                Latitude: 35.5,
                Longitude: 139.8),
        };

        var markers = CesiumBillboardProjection.Project(symbols);

        Assert.That(markers, Has.Count.EqualTo(1));
        Assert.That(markers[0].Latitude, Is.EqualTo(35.5).Within(1e-6));
        Assert.That(markers[0].Longitude, Is.EqualTo(139.8).Within(1e-6));
    }

    [Test]
    public void Project_falls_back_to_baltic_hash_without_lat_lon()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "F", "Blue", 0.5f, 0.5f, false),
        };

        var markers = CesiumBillboardProjection.Project(symbols, layoutSeed: 7);

        Assert.That(markers, Has.Count.EqualTo(1));
        // Baltic bbox: lat 59.5–60.5, lon 24.0–25.5
        Assert.That(markers[0].Latitude, Is.InRange(59.5, 60.5));
        Assert.That(markers[0].Longitude, Is.InRange(24.0, 25.5));
    }

    [Test]
    public void ProjectWithCamera_attaches_distance_labels()
    {
        var camera = GlobeViewProjection.BalticTheater().Camera;
        var symbols = new[]
        {
            new MapSymbolEntry(
                "f1",
                "Friendly",
                "F",
                "Blue",
                0f,
                0f,
                false,
                Latitude: 60.0,
                Longitude: 25.0),
            new MapSymbolEntry(
                "h1",
                "Hostile",
                "H",
                "Red",
                0f,
                0f,
                false,
                Latitude: 61.0,
                Longitude: 26.0),
        };

        var markers = CesiumBillboardProjection.ProjectWithCamera(symbols, camera);

        Assert.That(markers, Has.Count.EqualTo(2));
        Assert.That(markers[0].DistanceLabel, Is.Not.Null.And.Not.Empty);
        Assert.That(markers[1].DistanceLabel, Is.Not.Null.And.Not.Empty);
    }
}
