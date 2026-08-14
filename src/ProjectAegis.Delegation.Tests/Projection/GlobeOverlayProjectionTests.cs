using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class GlobeOverlayProjectionTests
{
    [Test]
    public void ProjectRings_empty_when_no_entries()
    {
        var symbols = new[] { new MapSymbolEntry("u1", "Friendly", "F", "u1", 0.2f, 0.3f, false) };

        var rings = GlobeOverlayProjection.ProjectRings(Array.Empty<EnvelopeRingEntry>(), symbols);

        Assert.That(rings, Is.Empty);
    }

    [Test]
    public void ProjectRings_builds_closed_polyline_for_selected_unit()
    {
        var symbols = new[]
        {
            new MapSymbolEntry(
                "u1",
                "Friendly",
                "F",
                "u1",
                0.2f,
                0.3f,
                false,
                Latitude: 60.0,
                Longitude: 25.0),
        };

        var entries = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
            "u1",
            sensorRangeNm: 40,
            weaponRangeNm: 20);

        var rings = GlobeOverlayProjection.ProjectRings(entries, symbols, ringSegments: 8);

        Assert.That(rings, Has.Count.EqualTo(2));
        var sensor = rings.Single(r => r.RingKind == TacticalOverlayProjection.RingKindSensor);
        Assert.That(sensor.CenterLatitude, Is.EqualTo(60.0).Within(0.001));
        Assert.That(sensor.CenterLongitude, Is.EqualTo(25.0).Within(0.001));
        Assert.That(sensor.Polyline, Has.Count.EqualTo(9));
        Assert.That(sensor.Polyline[0], Is.EqualTo(sensor.Polyline[^1]));
    }

    [Test]
    public void ProjectRings_skips_unknown_unit_id()
    {
        var symbols = new[] { new MapSymbolEntry("u1", "Friendly", "F", "u1", 0.2f, 0.3f, false) };
        var entries = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
            "missing",
            sensorRangeNm: 40,
            weaponRangeNm: 20);

        var rings = GlobeOverlayProjection.ProjectRings(entries, symbols);

        Assert.That(rings, Is.Empty);
    }

    [Test]
    public void ProjectEdges_resolves_endpoints_from_symbols()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "F", "u1", 0.1f, 0.2f, false, Latitude: 60.0, Longitude: 24.0),
            new MapSymbolEntry("u2", "Friendly", "F", "u2", 0.5f, 0.6f, false, Latitude: 59.9, Longitude: 24.5),
        };

        var edges = new[]
        {
            new DatalinkEdgeEntry("u1", "u2", "tactical", DatalinkPictureProjection.StatusUp),
        };

        var projected = GlobeOverlayProjection.ProjectEdges(edges, symbols);

        Assert.That(projected, Has.Count.EqualTo(1));
        Assert.That(projected[0].FromLatitude, Is.EqualTo(60.0).Within(0.001));
        Assert.That(projected[0].ToLongitude, Is.EqualTo(24.5).Within(0.001));
    }

    [Test]
    public void BuildRingPolyline_returns_empty_for_non_positive_radius()
    {
        var polyline = GlobeOverlayProjection.BuildRingPolyline(60, 25, rangeNm: 0, segments: 8);

        Assert.That(polyline, Is.Empty);
    }

    [Test]
    public void TryResolveUnitGeo_prefers_explicit_wgs84()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "F", "u1", 0.1f, 0.2f, false, Latitude: 61.0, Longitude: 26.0),
        };

        var ok = GlobeOverlayProjection.TryResolveUnitGeo("u1", symbols, out var lat, out var lon);

        Assert.That(ok, Is.True);
        Assert.That(lat, Is.EqualTo(61.0));
        Assert.That(lon, Is.EqualTo(26.0));
    }
}
