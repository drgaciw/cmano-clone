using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// TR-c2-004: lat/lon ↔ NDC mapping used by globe drag-box and pick
/// (document: NormalizedX = lon fraction, NormalizedY = lat fraction).
/// </summary>
[TestFixture]
public sealed class GlobeCoordinateMapperTests
{
    private static readonly GeographicBounds Baltic = TheaterQuickJump.BalticBounds;

    [Test]
    public void ToNormalized_maps_southwest_corner_to_origin()
    {
        var ndc = GlobeCoordinateMapper.ToNormalized(Baltic.MinLatitude, Baltic.MinLongitude, Baltic);
        Assert.That(ndc, Is.Not.Null);
        Assert.That(ndc!.Value.NormalizedX, Is.EqualTo(0f).Within(1e-5f));
        Assert.That(ndc.Value.NormalizedY, Is.EqualTo(0f).Within(1e-5f));
    }

    [Test]
    public void ToNormalized_maps_northeast_corner_to_one()
    {
        var ndc = GlobeCoordinateMapper.ToNormalized(Baltic.MaxLatitude, Baltic.MaxLongitude, Baltic);
        Assert.That(ndc, Is.Not.Null);
        Assert.That(ndc!.Value.NormalizedX, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(ndc.Value.NormalizedY, Is.EqualTo(1f).Within(1e-5f));
    }

    [Test]
    public void ToGeographic_is_inverse_of_ToNormalized()
    {
        const float x = 0.4f;
        const float y = 0.7f;
        var geo = GlobeCoordinateMapper.ToGeographic(x, y, Baltic);
        Assert.That(geo, Is.Not.Null);

        var back = GlobeCoordinateMapper.ToNormalized(geo!.Value.Latitude, geo.Value.Longitude, Baltic);
        Assert.That(back!.Value.NormalizedX, Is.EqualTo(x).Within(1e-5f));
        Assert.That(back.Value.NormalizedY, Is.EqualTo(y).Within(1e-5f));
    }

    [Test]
    public void ToNormalizedRect_normalizes_arbitrary_lat_lon_corners_for_SelectionBoxResolver()
    {
        // SW then NE in geo → NDC rect covering center of Baltic.
        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            latitude0: 59.7,
            longitude0: 24.3,
            latitude1: 60.3,
            longitude1: 25.0,
            Baltic);

        Assert.That(rect, Is.Not.Null);
        Assert.That(rect!.Value.MinX, Is.LessThan(rect.Value.MaxX));
        Assert.That(rect.Value.MinY, Is.LessThan(rect.Value.MaxY));
        Assert.That(rect.Value.IsDrag, Is.True);

        // Flip corner order — same rect.
        var flipped = GlobeCoordinateMapper.ToNormalizedRect(60.3, 25.0, 59.7, 24.3, Baltic);
        Assert.That(flipped!.Value, Is.EqualTo(rect.Value));
    }

    [Test]
    public void SymbolToGeographic_matches_CesiumBillboardProjection_Baltic_mapping()
    {
        var symbol = new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.5f, 0.25f, false);
        var geo = GlobeCoordinateMapper.SymbolToGeographic(symbol, Baltic);
        Assert.That(geo, Is.Not.Null);

        var markers = CesiumBillboardProjection.Project(new[] { symbol }, layoutSeed: 7);
        Assert.That(markers, Has.Count.EqualTo(1));
        Assert.That(geo!.Value.Latitude, Is.EqualTo(markers[0].Latitude).Within(1e-6));
        Assert.That(geo.Value.Longitude, Is.EqualTo(markers[0].Longitude).Within(1e-6));
    }

    [Test]
    public void ToNormalized_returns_null_for_invalid_zero_span_bounds()
    {
        var invalid = new GeographicBounds(10, 20, 10, 20);
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(GlobeCoordinateMapper.ToNormalized(10, 20, invalid), Is.Null);
    }
}
