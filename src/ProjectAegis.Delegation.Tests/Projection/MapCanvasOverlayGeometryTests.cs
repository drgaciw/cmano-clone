using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapCanvasOverlayGeometryTests
{
    private static MapSymbolDisplayRow Symbol(string id, float x, float y, bool ghost = false) =>
        new(id, "■", id, x, y, "map-symbol--friendly", false, ghost);

    [Test]
    public void NmToNormalizedRadius_scales_by_theater_width()
    {
        Assert.That(
            MapCanvasOverlayGeometry.NmToNormalizedRadius(400, 800),
            Is.EqualTo(0.5f).Within(1e-6f));
    }

    [Test]
    public void BuildUnitPositionIndex_skips_ghost_rows()
    {
        var positions = MapCanvasOverlayGeometry.BuildUnitPositionIndex(
        [
            Symbol("u1", 0.2f, 0.3f),
            Symbol("ghost:u1", 0.9f, 0.9f, ghost: true),
            Symbol("u2", 0.6f, 0.4f),
        ]);

        Assert.That(positions.Count, Is.EqualTo(2));
        Assert.That(positions["u1"], Is.EqualTo((0.2f, 0.3f)));
        Assert.That(positions["u2"], Is.EqualTo((0.6f, 0.4f)));
    }

    [Test]
    public void ProjectRings_places_sensor_and_weapon_at_unit_center()
    {
        var positions = MapCanvasOverlayGeometry.BuildUnitPositionIndex([Symbol("u1", 0.5f, 0.5f)]);
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("u1", 40, 20, "Surface");

        var shapes = MapCanvasOverlayGeometry.ProjectRings(rings, positions);

        Assert.That(shapes.Count, Is.EqualTo(2));
        Assert.That(shapes[0].CenterX, Is.EqualTo(0.5f).Within(1e-6f));
        Assert.That(shapes[0].CenterY, Is.EqualTo(0.5f).Within(1e-6f));
        Assert.That(shapes[0].StyleClass, Is.EqualTo(MapCanvasOverlayGeometry.RingStyleSensor));
        Assert.That(shapes[1].StyleClass, Is.EqualTo(MapCanvasOverlayGeometry.RingStyleWeapon));
        Assert.That(shapes[0].RadiusNormalized, Is.GreaterThan(shapes[1].RadiusNormalized));
    }

    [Test]
    public void ProjectEdges_draws_line_between_known_units()
    {
        var positions = MapCanvasOverlayGeometry.BuildUnitPositionIndex(
        [
            Symbol("u1", 0.2f, 0.2f),
            Symbol("u2", 0.8f, 0.8f),
        ]);
        var edges = DatalinkPictureProjection.Project(
        [
            ("u1", "u2", "tactical", DatalinkPictureProjection.StatusDegraded),
        ]);

        var shapes = MapCanvasOverlayGeometry.ProjectEdges(edges, positions);

        Assert.That(shapes.Count, Is.EqualTo(1));
        Assert.That(shapes[0].FromX, Is.EqualTo(0.2f).Within(1e-6f));
        Assert.That(shapes[0].ToX, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(shapes[0].StyleClass, Is.EqualTo(MapCanvasOverlayGeometry.EdgeStyleDegraded));
    }

    [Test]
    public void ProjectEdges_skips_missing_unit_positions()
    {
        var positions = MapCanvasOverlayGeometry.BuildUnitPositionIndex([Symbol("u1", 0.2f, 0.2f)]);
        var edges = DatalinkPictureProjection.Project(
        [
            ("u1", "missing", "tactical", DatalinkPictureProjection.StatusUp),
        ]);

        Assert.That(MapCanvasOverlayGeometry.ProjectEdges(edges, positions), Is.Empty);
    }

    [Test]
    public void LayoutRingPixels_stays_circular_on_wide_canvas()
    {
        var shape = new MapCanvasRingShape(
            "u1:sensor",
            CenterX: 0.5f,
            CenterY: 0.5f,
            RadiusNormalized: 0.25f,
            RingKind: TacticalOverlayProjection.RingKindSensor,
            StyleClass: MapCanvasOverlayGeometry.RingStyleSensor);

        var px = MapCanvasOverlayGeometry.LayoutRingPixels(shape, canvasWidth: 800f, canvasHeight: 400f);

        Assert.That(px.Diameter, Is.EqualTo(400f).Within(1e-4f));
        Assert.That(px.Left, Is.EqualTo(200f).Within(1e-4f));
        Assert.That(px.Top, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void LayoutRingPixels_square_canvas_matches_percent_math()
    {
        var shape = new MapCanvasRingShape(
            "u1:sensor",
            CenterX: 0.5f,
            CenterY: 0.5f,
            RadiusNormalized: 0.25f,
            RingKind: TacticalOverlayProjection.RingKindSensor,
            StyleClass: MapCanvasOverlayGeometry.RingStyleSensor);

        var px = MapCanvasOverlayGeometry.LayoutRingPixels(shape, canvasWidth: 400f, canvasHeight: 400f);

        Assert.That(px.Diameter, Is.EqualTo(200f).Within(1e-4f));
        Assert.That(px.Left, Is.EqualTo(100f).Within(1e-4f));
        Assert.That(px.Top, Is.EqualTo(100f).Within(1e-4f));
    }

    [Test]
    public void LayoutEdgePixels_uses_pixel_aspect_for_vertical_span()
    {
        var shape = new MapCanvasEdgeShape(
            "u1->u2",
            FromX: 0.5f,
            FromY: 0f,
            ToX: 0.5f,
            ToY: 1f,
            Status: DatalinkPictureProjection.StatusUp,
            StyleClass: MapCanvasOverlayGeometry.EdgeStyleUp);

        var px = MapCanvasOverlayGeometry.LayoutEdgePixels(shape, canvasWidth: 800f, canvasHeight: 400f);

        Assert.That(px.Hidden, Is.False);
        Assert.That(px.Length, Is.EqualTo(400f).Within(1e-4f));
        Assert.That(px.AngleDeg, Is.EqualTo(90f).Within(1e-3f));
        Assert.That(px.Left, Is.EqualTo(400f).Within(1e-4f));
        Assert.That(px.Top, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void LayoutEdgePixels_hides_zero_length()
    {
        var shape = new MapCanvasEdgeShape(
            "u1->u1",
            FromX: 0.2f,
            FromY: 0.3f,
            ToX: 0.2f,
            ToY: 0.3f,
            Status: DatalinkPictureProjection.StatusUp,
            StyleClass: MapCanvasOverlayGeometry.EdgeStyleUp);

        var px = MapCanvasOverlayGeometry.LayoutEdgePixels(shape, canvasWidth: 800f, canvasHeight: 400f);

        Assert.That(px.Hidden, Is.True);
        Assert.That(px.Length, Is.EqualTo(0f));
    }
}
