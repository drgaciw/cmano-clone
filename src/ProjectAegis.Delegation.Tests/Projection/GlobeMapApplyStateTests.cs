using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeMapApplyStateTests
{
    [Test]
    public void Apply_formats_status_line_globe_theater_markers_mode()
    {
        var view = GlobeViewProjection.DefaultBalticTheater();
        var markers = CesiumBillboardProjection.ProjectDemoPair();

        var applied = GlobeMapApplyState.Apply(view, markers);

        Assert.That(applied.StatusLine, Is.EqualTo("GLOBE · Baltic · 2 markers · 3D"));
        Assert.That(applied.TheaterLabel, Is.EqualTo("Baltic"));
        Assert.That(applied.MarkerCount, Is.EqualTo(2));
        Assert.That(applied.ModeLabel, Is.EqualTo("3D"));
        Assert.That(applied.ActiveBookmarkId, Is.EqualTo(GlobeViewProjection.BalticBookmarkId));
    }

    [Test]
    public void Apply_after_quick_jump_to_pacific_updates_status_theater()
    {
        var view = GlobeViewProjection.WithQuickJump(
            GlobeViewProjection.DefaultBalticTheater(),
            GlobeViewProjection.PacificBookmarkId);
        var markers = CesiumBillboardProjection.ProjectSeed();

        var applied = GlobeMapApplyState.Apply(view, markers);

        Assert.That(applied.StatusLine, Is.EqualTo("GLOBE · Pacific · 1 markers · 3D"));
        Assert.That(applied.TheaterLabel, Is.EqualTo("Pacific"));
    }

    [Test]
    public void Apply_two_d_mode_label()
    {
        var view = GlobeViewProjection.WithMode(
            GlobeViewProjection.DefaultBalticTheater(),
            GlobeViewMode2d3d.TwoD);

        var applied = GlobeMapApplyState.Apply(view, Array.Empty<CesiumBillboardMarker>());

        Assert.That(applied.ModeLabel, Is.EqualTo("2D"));
        Assert.That(applied.StatusLine, Does.EndWith("· 2D"));
        Assert.That(applied.MarkerCount, Is.EqualTo(0));
    }

    [Test]
    public void Apply_null_view_uses_globe_defaults()
    {
        var applied = GlobeMapApplyState.Apply(null, null);

        Assert.That(applied.StatusLine, Is.EqualTo("GLOBE · Globe · 0 markers · 3D"));
        Assert.That(applied.MarkerCount, Is.EqualTo(0));
        Assert.That(applied.Bookmarks.IsEmpty, Is.True);
        Assert.That(applied.Bookmarks.IsDisabled, Is.False);
    }

    [Test]
    public void Apply_bookmarks_empty_honest_not_disabled()
    {
        var view = new GlobeViewState(
            Camera: new GlobeCameraState(0, 0, 0, 0, 0),
            Bookmarks: Array.Empty<GlobeTheaterBookmark>(),
            ActiveBookmarkId: null,
            Mode2d3d: GlobeViewMode2d3d.ThreeD);

        var applied = GlobeMapApplyState.Apply(view, Array.Empty<CesiumBillboardMarker>());

        Assert.That(applied.Bookmarks.IsEmpty, Is.True);
        Assert.That(applied.Bookmarks.IsDisabled, Is.False);
        Assert.That(applied.Bookmarks.EmptyStateLine, Is.EqualTo(GlobeViewProjection.EmptyBookmarksLine));
    }

    [Test]
    public void ProjectAndApply_projects_symbols_and_formats_status()
    {
        var view = GlobeViewProjection.DefaultBalticTheater();
        var symbols = new[]
        {
            new MapSymbolEntry("f1", "Friendly", "F", "Blue", 0.2f, 0.3f, false),
            new MapSymbolEntry("h1", "Hostile", "H", "Red", 0.7f, 0.6f, false),
        };

        var applied = GlobeMapApplyState.ProjectAndApply(view, symbols);

        Assert.That(applied.StatusLine, Is.EqualTo("GLOBE · Baltic · 2 markers · 3D"));
        Assert.That(applied.MarkerCount, Is.EqualTo(2));
        Assert.That(applied.Bookmarks.IsEmpty, Is.False);
        Assert.That(applied.Bookmarks.Bookmarks, Has.Count.EqualTo(3));
    }

    [Test]
    public void Apply_with_overlays_formats_status_line_with_ring_and_edge_counts()
    {
        var view = GlobeViewProjection.DefaultBalticTheater();
        var markers = CesiumBillboardProjection.ProjectDemoPair();
        var symbols = new[]
        {
            new MapSymbolEntry("demo-friendly", "Friendly", "F", "demo-friendly", 0.2f, 0.3f, false, Latitude: 60.17, Longitude: 24.94),
            new MapSymbolEntry("demo-hostile", "Hostile", "H", "demo-hostile", 0.7f, 0.6f, false, Latitude: 59.95, Longitude: 24.50),
        };

        var ringEntries = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
            "demo-friendly",
            sensorRangeNm: 40,
            weaponRangeNm: 20);
        var edgeEntries = new[]
        {
            new DatalinkEdgeEntry("demo-friendly", "demo-hostile", "tactical", DatalinkPictureProjection.StatusUp),
        };

        var ringMarkers = GlobeOverlayProjection.ProjectRings(ringEntries, symbols);
        var edgeMarkers = GlobeOverlayProjection.ProjectEdges(edgeEntries, symbols);

        var applied = GlobeMapApplyState.Apply(view, markers, ringMarkers, edgeMarkers);

        Assert.That(applied.StatusLine, Is.EqualTo("GLOBE · Baltic · 2 markers · 2 rings · 1 links · 3D"));
        Assert.That(applied.EnvelopeRingCount, Is.EqualTo(2));
        Assert.That(applied.DatalinkEdgeCount, Is.EqualTo(1));
        Assert.That(applied.EnvelopeRings, Has.Count.EqualTo(2));
        Assert.That(applied.DatalinkEdges, Has.Count.EqualTo(1));
    }

    [Test]
    public void Quick_jump_does_not_require_sim_and_is_pure()
    {
        // Document product rule: theater quick-jump is view-state only.
        var a = GlobeViewProjection.DefaultBalticTheater();
        var b = GlobeViewProjection.WithQuickJump(a, GlobeViewProjection.GiukBookmarkId);
        var again = GlobeViewProjection.WithQuickJump(a, GlobeViewProjection.GiukBookmarkId);

        Assert.That(b, Is.EqualTo(again));
        Assert.That(a.Camera.Latitude, Is.Not.EqualTo(b.Camera.Latitude));
        var applied = GlobeMapApplyState.Apply(b, CesiumBillboardProjection.ProjectDemoPair());
        Assert.That(applied.StatusLine, Does.Contain("GIUK"));
    }
}
