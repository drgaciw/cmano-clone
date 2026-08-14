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
}
